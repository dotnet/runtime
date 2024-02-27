// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        Amd64/x86 Code Generator                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#pragma warning(disable : 4310) // cast truncates constant value - happens for (int8_t)0xb1
#endif

#ifdef TARGET_XARCH
#include "emit.h"
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"
#include "patchpointinfo.h"

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

    if (compiler->opts.IsOSR() && compiler->info.compPatchpointInfo->HasSecurityCookie())
    {
        // Security cookie is on original frame and was initialized there.
        return;
    }

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
        noway_assert(compiler->gsGlobalSecurityCookieVal != 0);
#ifdef TARGET_AMD64
        if ((size_t)(int)compiler->gsGlobalSecurityCookieVal != compiler->gsGlobalSecurityCookieVal)
        {
            // initReg = #GlobalSecurityCookieVal64; [frame.GSSecurityCookie] = initReg
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, compiler->gsGlobalSecurityCookieVal);
            GetEmitter()->emitIns_S_R(INS_mov, EA_PTRSIZE, initReg, compiler->lvaGSSecurityCookie, 0);
            *pInitRegZeroed = false;
        }
        else
#endif
        {
            // mov   dword ptr [frame.GSSecurityCookie], #GlobalSecurityCookieVal
            GetEmitter()->emitIns_S_I(INS_mov, EA_PTRSIZE, compiler->lvaGSSecurityCookie, 0,
                                      (int)compiler->gsGlobalSecurityCookieVal);
        }
    }
    else
    {
        // Always use EAX on x86 and x64
        // On x64, if we're not moving into RAX, and the address isn't RIP relative, we can't encode it.
        //  mov   eax, dword ptr [compiler->gsGlobalSecurityCookieAddr]
        //  mov   dword ptr [frame.GSSecurityCookie], eax
        GetEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_EAX, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        regSet.verifyRegUsed(REG_EAX);
        GetEmitter()->emitIns_S_R(INS_mov, EA_PTRSIZE, REG_EAX, compiler->lvaGSSecurityCookie, 0);
        if (initReg == REG_EAX)
        {
            *pInitRegZeroed = false;
        }
    }
}

/*****************************************************************************
 *
 *   Generate code to check that the GS cookie wasn't thrashed by a buffer
 *   overrun.  If pushReg is true, preserve all registers around code sequence.
 *   Otherwise ECX could be modified.
 *
 *   Implementation Note: pushReg = true, in case of tail calls.
 */
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    noway_assert(compiler->gsGlobalSecurityCookieAddr || compiler->gsGlobalSecurityCookieVal);

    // Make sure that EAX is reported as live GC-ref so that any GC that kicks in while
    // executing GS cookie check will not collect the object pointed to by EAX.
    //
    // For Amd64 System V, a two-register-returned struct could be returned in RAX and RDX
    // In such case make sure that the correct GC-ness of RDX is reported as well, so
    // a GC object pointed by RDX will not be collected.
    if (!pushReg)
    {
        if (compiler->compMethodReturnsRetBufAddr())
        {
            // This is for returning in an implicit RetBuf.
            // If the address of the buffer is returned in REG_INTRET, mark the content of INTRET as ByRef.

            // In case the return is in an implicit RetBuf, the native return type should be a struct
            assert(varTypeIsStruct(compiler->info.compRetNativeType));

            gcInfo.gcMarkRegPtrVal(REG_INTRET, TYP_BYREF);
        }
        else
        {
            ReturnTypeDesc retTypeDesc = compiler->compRetTypeDesc;
            const unsigned regCount    = retTypeDesc.GetReturnRegCount();

            for (unsigned i = 0; i < regCount; ++i)
            {
                gcInfo.gcMarkRegPtrVal(retTypeDesc.GetABIReturnReg(i), retTypeDesc.GetReturnRegType(i));
            }
        }
    }

    regNumber regGSCheck;
    regMaskTP regMaskGSCheck = RBM_NONE;

    if (!pushReg)
    {
        // Non-tail call: we can use any callee trash register that is not
        // a return register or contain 'this' pointer (keep alive this), since
        // we are generating GS cookie check after a GT_RETURN block.
        // Note: On Amd64 System V RDX is an arg register - REG_ARG_2 - as well
        // as return register for two-register-returned structs.
        if (compiler->lvaKeepAliveAndReportThis() && compiler->lvaGetDesc(compiler->info.compThisArg)->lvIsInReg() &&
            (compiler->lvaGetDesc(compiler->info.compThisArg)->GetRegNum() == REG_ARG_0))
        {
            regGSCheck = REG_ARG_1;
        }
        else
        {
            regGSCheck = REG_ARG_0;
        }
    }
    else
    {
#ifdef TARGET_X86
        // It doesn't matter which register we pick, since we're going to save and restore it
        // around the check.
        // TODO-CQ: Can we optimize the choice of register to avoid doing the push/pop sometimes?
        regGSCheck     = REG_EAX;
        regMaskGSCheck = RBM_EAX;
#else  // !TARGET_X86
        // Jmp calls: specify method handle using which JIT queries VM for its entry point
        // address and hence it can neither be a VSD call nor PInvoke calli with cookie
        // parameter.  Therefore, in case of jmp calls it is safe to use R11.
        regGSCheck = REG_R11;
#endif // !TARGET_X86
    }

    regMaskTP byrefPushedRegs = RBM_NONE;
    regMaskTP norefPushedRegs = RBM_NONE;
    regMaskTP pushedRegs      = RBM_NONE;

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
#if defined(TARGET_AMD64)
        // If GS cookie value fits within 32-bits we can use 'cmp mem64, imm32'.
        // Otherwise, load the value into a reg and use 'cmp mem64, reg64'.
        if ((int)compiler->gsGlobalSecurityCookieVal != (ssize_t)compiler->gsGlobalSecurityCookieVal)
        {
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, regGSCheck, compiler->gsGlobalSecurityCookieVal);
            GetEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, regGSCheck, compiler->lvaGSSecurityCookie, 0);
        }
        else
#endif // defined(TARGET_AMD64)
        {
            assert((int)compiler->gsGlobalSecurityCookieVal == (ssize_t)compiler->gsGlobalSecurityCookieVal);
            GetEmitter()->emitIns_S_I(INS_cmp, EA_PTRSIZE, compiler->lvaGSSecurityCookie, 0,
                                      (int)compiler->gsGlobalSecurityCookieVal);
        }
    }
    else
    {
        // Ngen case - GS cookie value needs to be accessed through an indirection.

        pushedRegs = genPushRegs(regMaskGSCheck, &byrefPushedRegs, &norefPushedRegs);

        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, regGSCheck, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        GetEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, regGSCheck, regGSCheck, 0);
        GetEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, regGSCheck, compiler->lvaGSSecurityCookie, 0);
    }

    BasicBlock* gsCheckBlk = genCreateTempLabel();
    inst_JMP(EJ_je, gsCheckBlk);
    genEmitHelperCall(CORINFO_HELP_FAIL_FAST, 0, EA_UNKNOWN);
    genDefineTempLabel(gsCheckBlk);

    genPopRegs(pushedRegs, byrefPushedRegs, norefPushedRegs);
}

BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    assert(block->KindIs(BBJ_CALLFINALLY));

    BasicBlock* const nextBlock = block->Next();

#if defined(FEATURE_EH_FUNCLETS)
    // Generate a call to the finally, like this:
    //      mov         rcx,qword ptr [rbp + 20H]       // Load rcx with PSPSym
    //      call        finally-funclet
    //      jmp         finally-return                  // Only for non-retless finally calls
    // The jmp can be a NOP if we're going to the next block.
    // If we're generating code for the main function (not a funclet), and there is no localloc,
    // then RSP at this point is the same value as that stored in the PSPSym. So just copy RSP
    // instead of loading the PSPSym in this case, or if PSPSym is not used (NativeAOT ABI).

    if ((compiler->lvaPSPSym == BAD_VAR_NUM) ||
        (!compiler->compLocallocUsed && (compiler->funCurrentFunc()->funKind == FUNC_ROOT)))
    {
#ifndef UNIX_X86_ABI
        inst_Mov(TYP_I_IMPL, REG_ARG_0, REG_SPBASE, /* canSkip */ false);
#endif // !UNIX_X86_ABI
    }
    else
    {
        GetEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_ARG_0, compiler->lvaPSPSym, 0);
    }
    GetEmitter()->emitIns_J(INS_call, block->GetTarget());

    if (block->HasFlag(BBF_RETLESS_CALL))
    {
        // We have a retless call, and the last instruction generated was a call.
        // If the next block is in a different EH region (or is the end of the code
        // block), then we need to generate a breakpoint here (since it will never
        // get executed) to get proper unwind behavior.

        if ((nextBlock == nullptr) || !BasicBlock::sameEHRegion(block, nextBlock))
        {
            instGen(INS_BREAKPOINT); // This should never get executed
        }
    }
    else
    {
// TODO-Linux-x86: Do we need to handle the GC information for this NOP or JMP specially, as is done for other
// architectures?
#ifndef JIT32_GCENCODER
        // Because of the way the flowgraph is connected, the liveness info for this one instruction
        // after the call is not (can not be) correct in cases where a variable has a last use in the
        // handler.  So turn off GC reporting for this single instruction.
        GetEmitter()->emitDisableGC();
#endif // JIT32_GCENCODER

        BasicBlock* const finallyContinuation = nextBlock->GetFinallyContinuation();

        // Now go to where the finally funclet needs to return to.
        if (nextBlock->NextIs(finallyContinuation) && !compiler->fgInDifferentRegions(nextBlock, finallyContinuation))
        {
            // Fall-through.
            // TODO-XArch-CQ: Can we get rid of this instruction, and just have the call return directly
            // to the next instruction? This would depend on stack walking from within the finally
            // handler working without this instruction being in this special EH region.
            instGen(INS_nop);
        }
        else
        {
            inst_JMP(EJ_jmp, finallyContinuation);
        }

#ifndef JIT32_GCENCODER
        GetEmitter()->emitEnableGC();
#endif // JIT32_GCENCODER
    }

#else // !FEATURE_EH_FUNCLETS

    // If we are about to invoke a finally locally from a try block, we have to set the ShadowSP slot
    // corresponding to the finally's nesting level. When invoked in response to an exception, the
    // EE does this.
    //
    // We have a BBJ_CALLFINALLY possibly paired with a following BBJ_CALLFINALLYRET.
    //
    // We will emit :
    //      mov [ebp - (n + 1)], 0
    //      mov [ebp -  n     ], 0xFC
    //      push &step
    //      jmp  finallyBlock
    // ...
    // step:
    //      mov [ebp -  n     ], 0
    //      jmp leaveTarget
    // ...
    // leaveTarget:

    noway_assert(isFramePointerUsed());

    // Get the nesting level which contains the finally
    unsigned finallyNesting = 0;
    compiler->fgGetNestingLevel(block, &finallyNesting);

    // The last slot is reserved for ICodeManager::FixContext(ppEndRegion)
    unsigned filterEndOffsetSlotOffs;
    filterEndOffsetSlotOffs = (unsigned)(compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) - TARGET_POINTER_SIZE);

    unsigned curNestingSlotOffs;
    curNestingSlotOffs = (unsigned)(filterEndOffsetSlotOffs - ((finallyNesting + 1) * TARGET_POINTER_SIZE));

    // Zero out the slot for the next nesting level
    GetEmitter()->emitIns_S_I(INS_mov, EA_PTRSIZE, compiler->lvaShadowSPslotsVar,
                              curNestingSlotOffs - TARGET_POINTER_SIZE, 0);
    GetEmitter()->emitIns_S_I(INS_mov, EA_PTRSIZE, compiler->lvaShadowSPslotsVar, curNestingSlotOffs, LCL_FINALLY_MARK);

    // Now push the address where the finally funclet should return to directly.
    if (!block->HasFlag(BBF_RETLESS_CALL))
    {
        assert(block->isBBCallFinallyPair());
        GetEmitter()->emitIns_J(INS_push_hide, nextBlock->GetFinallyContinuation());
    }
    else
    {
        // EE expects a DWORD, so we provide 0
        inst_IV(INS_push_hide, 0);
    }

    // Jump to the finally BB
    inst_JMP(EJ_jmp, block->GetTarget());

#endif // !FEATURE_EH_FUNCLETS

    // The BBJ_CALLFINALLYRET is used because the BBJ_CALLFINALLY can't point to the
    // jump target using bbTarget - that is already used to point
    // to the finally block. So just skip past the BBJ_CALLFINALLYRET unless the
    // block is RETLESS.
    if (!block->HasFlag(BBF_RETLESS_CALL))
    {
        assert(block->isBBCallFinallyPair());
        block = nextBlock;
    }
    return block;
}

#if defined(FEATURE_EH_FUNCLETS)
void CodeGen::genEHCatchRet(BasicBlock* block)
{
    // Set RAX to the address the VM should return to after the catch.
    // Generate a RIP-relative
    //         lea reg, [rip + disp32] ; the RIP is implicit
    // which will be position-independent.
    GetEmitter()->emitIns_R_L(INS_lea, EA_PTR_DSP_RELOC, block->GetTarget(), REG_INTRET);
}

#else // !FEATURE_EH_FUNCLETS

void CodeGen::genEHFinallyOrFilterRet(BasicBlock* block)
{
    // The last statement of the block must be a GT_RETFILT, which has already been generated.
    assert(block->lastNode() != nullptr);
    assert(block->lastNode()->OperGet() == GT_RETFILT);

    if (block->KindIs(BBJ_EHFINALLYRET, BBJ_EHFAULTRET))
    {
        assert(block->lastNode()->AsOp()->gtOp1 == nullptr); // op1 == nullptr means endfinally

        // Return using a pop-jmp sequence. As the "try" block calls
        // the finally with a jmp, this leaves the x86 call-ret stack
        // balanced in the normal flow of path.

        noway_assert(isFramePointerRequired());
        inst_RV(INS_pop_hide, REG_EAX, TYP_I_IMPL);
        inst_RV(INS_i_jmp, REG_EAX, TYP_I_IMPL);
    }
    else
    {
        assert(block->KindIs(BBJ_EHFILTERRET));

        // The return value has already been computed.
        instGen_Return(0);
    }
}

#endif // !FEATURE_EH_FUNCLETS

//  Move an immediate value into an integer register

void CodeGen::instGen_Set_Reg_To_Imm(emitAttr  size,
                                     regNumber reg,
                                     ssize_t   imm,
                                     insFlags flags DEBUGARG(size_t targetHandle) DEBUGARG(GenTreeFlags gtFlags))
{
    // reg cannot be a FP register
    assert(!genIsValidFloatReg(reg));

    emitAttr origAttr = size;
    if (!compiler->opts.compReloc)
    {
        // Strip any reloc flags from size if we aren't doing relocs
        size = EA_REMOVE_FLG(size, EA_CNS_RELOC_FLG | EA_DSP_RELOC_FLG);
    }

    if ((imm == 0) && !EA_IS_RELOC(size))
    {
        instGen_Set_Reg_To_Zero(size, reg, flags);
    }
    else
    {
        // Only use lea if the original was relocatable. Otherwise we can get spurious
        // instruction selection due to different memory placement at runtime.
        if (EA_IS_RELOC(origAttr) && genDataIndirAddrCanBeEncodedAsPCRelOffset(imm))
        {
            if (EA_IS_CNS_TLSGD_RELOC(origAttr))
            {
                // NativeAOT code needs special code sequence prefix of call so the
                // linker will do the fixup and emit accurate TLS access information.
                GetEmitter()->emitIns_Data16();
            }
            if (!EA_IS_CNS_SEC_RELOC(origAttr))
            {
                // We will use lea so displacement and not immediate will be relocatable
                size = EA_SET_FLG(EA_REMOVE_FLG(size, EA_CNS_RELOC_FLG), EA_DSP_RELOC_FLG);
                GetEmitter()->emitIns_R_AI(INS_lea, size, reg, imm DEBUGARG(targetHandle) DEBUGARG(gtFlags));
            }
            else
            {
                // For section constant, the immediate will be relocatable
                GetEmitter()->emitIns_R_I(INS_mov, size, reg, imm DEBUGARG(targetHandle) DEBUGARG(gtFlags));
            }
        }
        else
        {
            GetEmitter()->emitIns_R_I(INS_mov, size, reg, imm DEBUGARG(targetHandle) DEBUGARG(gtFlags));
        }
    }
    regSet.verifyRegUsed(reg);
}

#if defined(FEATURE_SIMD)
//----------------------------------------------------------------------------------
// genSetRegToConst: generate code to set target SIMD register to a given constant value
//
// Arguments:
//    targetReg  - target SIMD register
//    targetType - target's type
//    simd_t     - constant data (its width depends on type)
//
void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, simd_t* val)
{
    emitter* emit = GetEmitter();
    emitAttr attr = emitTypeSize(targetType);

    switch (targetType)
    {
        case TYP_SIMD8:
        {
            simd8_t val8 = *(simd8_t*)val;
            if (val8.IsAllBitsSet())
            {
                if (emitter::isHighSimdReg(targetReg))
                {
                    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    emit->emitIns_SIMD_R_R_R_I(INS_vpternlogd, attr, targetReg, targetReg, targetReg,
                                               static_cast<int8_t>(0xFF));
                }
                else
                {
                    emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, EA_16BYTE, targetReg, targetReg, targetReg);
                }
            }
            else if (val8.IsZero())
            {
                emit->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, targetReg, targetReg, targetReg);
            }
            else
            {
                CORINFO_FIELD_HANDLE hnd = emit->emitSimd8Const(val8);
                emit->emitIns_R_C(ins_Load(targetType), attr, targetReg, hnd, 0);
            }
            break;
        }
        case TYP_SIMD12:
        {
            simd12_t val12 = *(simd12_t*)val;
            if (val12.IsAllBitsSet())
            {
                if (emitter::isHighSimdReg(targetReg))
                {
                    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    emit->emitIns_SIMD_R_R_R_I(INS_vpternlogd, attr, targetReg, targetReg, targetReg,
                                               static_cast<int8_t>(0xFF));
                }
                else
                {
                    emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, EA_16BYTE, targetReg, targetReg, targetReg);
                }
            }
            else if (val12.IsZero())
            {
                emit->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, targetReg, targetReg, targetReg);
            }
            else
            {
                simd16_t val16 = {};
                memcpy(&val16, &val12, sizeof(val12));
                CORINFO_FIELD_HANDLE hnd = emit->emitSimd16Const(val16);
                emit->emitIns_R_C(ins_Load(targetType), attr, targetReg, hnd, 0);
            }
            break;
        }
        case TYP_SIMD16:
        {
            simd16_t val16 = *(simd16_t*)val;
            if (val16.IsAllBitsSet())
            {
                if (emitter::isHighSimdReg(targetReg))
                {
                    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    emit->emitIns_SIMD_R_R_R_I(INS_vpternlogd, attr, targetReg, targetReg, targetReg,
                                               static_cast<int8_t>(0xFF));
                }
                else
                {
                    emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, attr, targetReg, targetReg, targetReg);
                }
            }
            else if (val16.IsZero())
            {
                emit->emitIns_SIMD_R_R_R(INS_xorps, attr, targetReg, targetReg, targetReg);
            }
            else
            {
                CORINFO_FIELD_HANDLE hnd = emit->emitSimd16Const(val16);
                emit->emitIns_R_C(ins_Load(targetType), attr, targetReg, hnd, 0);
            }
            break;
        }
        case TYP_SIMD32:
        {
            simd32_t val32 = *(simd32_t*)val;
            if (val32.IsAllBitsSet() && compiler->compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                if (emitter::isHighSimdReg(targetReg))
                {
                    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    emit->emitIns_SIMD_R_R_R_I(INS_vpternlogd, attr, targetReg, targetReg, targetReg,
                                               static_cast<int8_t>(0xFF));
                }
                else
                {
                    emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, attr, targetReg, targetReg, targetReg);
                }
            }
            else if (val32.IsZero())
            {
                emit->emitIns_SIMD_R_R_R(INS_xorps, attr, targetReg, targetReg, targetReg);
            }
            else
            {
                CORINFO_FIELD_HANDLE hnd = emit->emitSimd32Const(val32);
                emit->emitIns_R_C(ins_Load(targetType), attr, targetReg, hnd, 0);
            }
            break;
        }
        case TYP_SIMD64:
        {
            simd64_t val64 = *(simd64_t*)val;
            if (val64.IsAllBitsSet() && compiler->compOpportunisticallyDependsOn(InstructionSet_AVX512F))
            {
                emit->emitIns_SIMD_R_R_R_I(INS_vpternlogd, attr, targetReg, targetReg, targetReg,
                                           static_cast<int8_t>(0xFF));
            }
            else if (val64.IsZero())
            {
                // Use VEX version because it's smaller (for zmm0-zmm15) than EVEX to zero a zmm register and still
                // zeros the entire register:
                //
                //   xorps zmm0, zmm0, zmm0 (6 bytes)
                //   xorps ymm0, ymm0, ymm0 (4 bytes)
                //
                emit->emitIns_SIMD_R_R_R(INS_xorps, EA_32BYTE, targetReg, targetReg, targetReg);
            }
            else
            {
                CORINFO_FIELD_HANDLE hnd = emit->emitSimd64Const(val64);
                emit->emitIns_R_C(ins_Load(targetType), attr, targetReg, hnd, 0);
            }
            break;
        }
        default:
        {
            unreached();
        }
    }
}
#endif // FEATURE_SIMD

/***********************************************************************************
 *
 * Generate code to set a register 'targetReg' of type 'targetType' to the constant
 * specified by the constant (GT_CNS_INT, GT_CNS_DBL, or GT_CNS_VEC) in 'tree'. This
 * does not call genProduceReg() on the target register.
 */
void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, GenTree* tree)
{
    switch (tree->gtOper)
    {
        case GT_CNS_INT:
        {
            // relocatable values tend to come down as a CNS_INT of native int type
            // so the line between these two opcodes is kind of blurry
            GenTreeIntCon* con    = tree->AsIntCon();
            ssize_t        cnsVal = con->IconValue();

            emitAttr attr = emitActualTypeSize(targetType);
            // Currently this cannot be done for all handles due to
            // https://github.com/dotnet/runtime/issues/60712. However, it is
            // also unclear whether we unconditionally want to use rip-relative
            // lea instructions when not necessary. While a mov is larger, on
            // many Intel CPUs rip-relative lea instructions have higher
            // latency.
            if (con->ImmedValNeedsReloc(compiler))
            {
                attr = EA_SET_FLG(attr, EA_CNS_RELOC_FLG);
            }

            if (targetType == TYP_BYREF)
            {
                attr = EA_SET_FLG(attr, EA_BYREF_FLG);
            }

            if (compiler->IsTargetAbi(CORINFO_NATIVEAOT_ABI))
            {
                if (con->IsIconHandle(GTF_ICON_SECREL_OFFSET))
                {
                    attr = EA_SET_FLG(attr, EA_CNS_SEC_RELOC);
                }
                else if (con->IsIconHandle(GTF_ICON_TLSGD_OFFSET))
                {
                    attr = EA_SET_FLG(attr, EA_CNS_TLSGD_RELOC);
                }
            }

            instGen_Set_Reg_To_Imm(attr, targetReg, cnsVal,
                                   INS_FLAGS_DONT_CARE DEBUGARG(con->gtTargetHandle) DEBUGARG(con->gtFlags));
            regSet.verifyRegUsed(targetReg);
        }
        break;

        case GT_CNS_DBL:
        {
            emitter* emit = GetEmitter();
            emitAttr size = emitTypeSize(targetType);

            if (tree->IsFloatPositiveZero())
            {
                // A faster/smaller way to generate Zero
                emit->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, targetReg, targetReg, targetReg);
            }
            else if (tree->IsFloatAllBitsSet())
            {
                if (emitter::isHighSimdReg(targetReg))
                {
                    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    emit->emitIns_SIMD_R_R_R_I(INS_vpternlogd, EA_16BYTE, targetReg, targetReg, targetReg,
                                               static_cast<int8_t>(0xFF));
                }
                else
                {
                    // A faster/smaller way to generate AllBitsSet
                    emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, EA_16BYTE, targetReg, targetReg, targetReg);
                }
            }
            else
            {
                double               cns = tree->AsDblCon()->DconValue();
                CORINFO_FIELD_HANDLE hnd = emit->emitFltOrDblConst(cns, size);

                emit->emitIns_R_C(ins_Load(targetType), size, targetReg, hnd, 0);
            }
        }
        break;

        case GT_CNS_VEC:
        {
#if defined(FEATURE_SIMD)
            GenTreeVecCon* vecCon = tree->AsVecCon();
            genSetRegToConst(vecCon->GetRegNum(), targetType, &vecCon->gtSimdVal);
#else
            unreached();
#endif
            break;
        }

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// genCodeForNegNot: Produce code for a GT_NEG/GT_NOT node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForNegNot(GenTree* tree)
{
    assert(tree->OperIs(GT_NEG, GT_NOT));

    regNumber targetReg  = tree->GetRegNum();
    var_types targetType = tree->TypeGet();

    if (varTypeIsFloating(targetType))
    {
        assert(tree->gtOper == GT_NEG);
        genSSE2BitwiseOp(tree);
    }
    else
    {
        GenTree* operand = tree->gtGetOp1();
        assert(operand->isUsedFromReg());
        regNumber operandReg = genConsumeReg(operand);

        inst_Mov(targetType, targetReg, operandReg, /* canSkip */ true);

        instruction ins = genGetInsForOper(tree->OperGet(), targetType);
        inst_RV(ins, targetReg, targetType);
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForBswap: Produce code for a GT_BSWAP / GT_BSWAP16 node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForBswap(GenTree* tree)
{
    assert(tree->OperIs(GT_BSWAP, GT_BSWAP16));

    regNumber targetReg  = tree->GetRegNum();
    var_types targetType = tree->TypeGet();

    GenTree* operand = tree->gtGetOp1();

    genConsumeRegs(operand);

    if (operand->isUsedFromReg())
    {
        inst_Mov(targetType, targetReg, operand->GetRegNum(), /* canSkip */ true);

        if (tree->OperIs(GT_BSWAP))
        {
            // 32-bit and 64-bit byte swaps use "bswap reg"
            inst_RV(INS_bswap, targetReg, targetType);
        }
        else
        {
            // 16-bit byte swaps use "ror reg.16, 8"
            inst_RV_IV(INS_ror_N, targetReg, 8 /* val */, emitAttr::EA_2BYTE);
        }
    }
    else
    {
        GetEmitter()->emitInsBinary(INS_movbe, emitTypeSize(operand), tree, operand);
    }

    if (tree->OperIs(GT_BSWAP16) && !genCanOmitNormalizationForBswap16(tree))
    {
        GetEmitter()->emitIns_Mov(INS_movzx, EA_2BYTE, targetReg, targetReg, /* canSkip */ false);
    }

    genProduceReg(tree);
}

// Produce code for a GT_INC_SATURATE node.
void CodeGen::genCodeForIncSaturate(GenTree* tree)
{
    regNumber targetReg  = tree->GetRegNum();
    var_types targetType = tree->TypeGet();

    GenTree* operand = tree->gtGetOp1();
    assert(operand->isUsedFromReg());
    regNumber operandReg = genConsumeReg(operand);

    inst_Mov(targetType, targetReg, operandReg, /* canSkip */ true);
    inst_RV_IV(INS_add, targetReg, 1, emitActualTypeSize(targetType));
    inst_RV_IV(INS_sbb, targetReg, 0, emitActualTypeSize(targetType));

    genProduceReg(tree);
}

// Generate code to get the high N bits of a N*N=2N bit multiplication result
void CodeGen::genCodeForMulHi(GenTreeOp* treeNode)
{
    assert(!treeNode->gtOverflowEx());

    regNumber targetReg  = treeNode->GetRegNum();
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = GetEmitter();
    emitAttr  size       = emitTypeSize(treeNode);
    GenTree*  op1        = treeNode->AsOp()->gtOp1;
    GenTree*  op2        = treeNode->AsOp()->gtOp2;

    // to get the high bits of the multiply, we are constrained to using the
    // 1-op form:  RDX:RAX = RAX * rm
    // The 3-op form (Rx=Ry*Rz) does not support it.

    genConsumeOperands(treeNode->AsOp());

    GenTree* regOp = op1;
    GenTree* rmOp  = op2;

    // Set rmOp to the memory operand (if any)
    if (op1->isUsedFromMemory() || (op2->isUsedFromReg() && (op2->GetRegNum() == REG_RAX)))
    {
        regOp = op2;
        rmOp  = op1;
    }
    assert(regOp->isUsedFromReg());

    // Setup targetReg when neither of the source operands was a matching register
    inst_Mov(targetType, REG_RAX, regOp->GetRegNum(), /* canSkip */ true);

    instruction ins;
    if ((treeNode->gtFlags & GTF_UNSIGNED) == 0)
    {
        ins = INS_imulEAX;
    }
    else
    {
        ins = INS_mulEAX;
    }
    emit->emitInsBinary(ins, size, treeNode, rmOp);

    // Move the result to the desired register, if necessary
    if (treeNode->OperGet() == GT_MULHI)
    {
        inst_Mov(targetType, targetReg, REG_RDX, /* canSkip */ true);
    }

    genProduceReg(treeNode);
}

#ifdef TARGET_X86
//------------------------------------------------------------------------
// genCodeForLongUMod: Generate code for a tree of the form
//                     `(umod (gt_long x y) (const int))`
//
// Arguments:
//   node - the node for which to generate code
//
void CodeGen::genCodeForLongUMod(GenTreeOp* node)
{
    assert(node != nullptr);
    assert(node->OperGet() == GT_UMOD);
    assert(node->TypeGet() == TYP_INT);

    GenTreeOp* const dividend = node->gtOp1->AsOp();
    assert(dividend->OperGet() == GT_LONG);
    assert(varTypeIsLong(dividend));

    genConsumeOperands(node);

    GenTree* const dividendLo = dividend->gtOp1;
    GenTree* const dividendHi = dividend->gtOp2;
    assert(dividendLo->isUsedFromReg());
    assert(dividendHi->isUsedFromReg());

    GenTree* const divisor = node->gtOp2;
    assert(divisor->gtSkipReloadOrCopy()->OperGet() == GT_CNS_INT);
    assert(divisor->gtSkipReloadOrCopy()->isUsedFromReg());
    assert(divisor->gtSkipReloadOrCopy()->AsIntCon()->gtIconVal >= 2);
    assert(divisor->gtSkipReloadOrCopy()->AsIntCon()->gtIconVal <= 0x3fffffff);

    // dividendLo must be in RAX; dividendHi must be in RDX
    genCopyRegIfNeeded(dividendLo, REG_EAX);
    genCopyRegIfNeeded(dividendHi, REG_EDX);

    // At this point, EAX:EDX contains the 64bit dividend and op2->GetRegNum()
    // contains the 32bit divisor. We want to generate the following code:
    //
    //   cmp edx, divisor->GetRegNum()
    //   jb noOverflow
    //
    //   mov temp, eax
    //   mov eax, edx
    //   xor edx, edx
    //   div divisor->GetRegNum()
    //   mov eax, temp
    //
    // noOverflow:
    //   div divisor->GetRegNum()
    //
    // This works because (a * 2^32 + b) % c = ((a % c) * 2^32 + b) % c.

    BasicBlock* const noOverflow = genCreateTempLabel();

    //   cmp edx, divisor->GetRegNum()
    //   jb noOverflow
    inst_RV_RV(INS_cmp, REG_EDX, divisor->GetRegNum());
    inst_JMP(EJ_jb, noOverflow);

    //   mov temp, eax
    //   mov eax, edx
    //   xor edx, edx
    //   div divisor->GetRegNum()
    //   mov eax, temp
    const regNumber tempReg = node->GetSingleTempReg();
    inst_Mov(TYP_INT, tempReg, REG_EAX, /* canSkip */ false);
    inst_Mov(TYP_INT, REG_EAX, REG_EDX, /* canSkip */ false);
    instGen_Set_Reg_To_Zero(EA_PTRSIZE, REG_EDX);
    inst_RV(INS_div, divisor->GetRegNum(), TYP_INT);
    inst_Mov(TYP_INT, REG_EAX, tempReg, /* canSkip */ false);

    // noOverflow:
    //   div divisor->GetRegNum()
    genDefineTempLabel(noOverflow);
    inst_RV(INS_div, divisor->GetRegNum(), TYP_INT);

    const regNumber targetReg = node->GetRegNum();
    inst_Mov(TYP_INT, targetReg, REG_RDX, /* canSkip */ true);
    genProduceReg(node);
}
#endif // TARGET_X86

//------------------------------------------------------------------------
// genCodeForDivMod: Generate code for a DIV or MOD operation.
//
// Arguments:
//    treeNode - the node to generate the code for
//
void CodeGen::genCodeForDivMod(GenTreeOp* treeNode)
{
    assert(treeNode->OperIs(GT_DIV, GT_UDIV, GT_MOD, GT_UMOD));

    GenTree* dividend = treeNode->gtOp1;

#ifdef TARGET_X86
    if (varTypeIsLong(dividend->TypeGet()))
    {
        genCodeForLongUMod(treeNode);
        return;
    }
#endif // TARGET_X86

    GenTree*   divisor    = treeNode->gtOp2;
    genTreeOps oper       = treeNode->OperGet();
    emitAttr   size       = emitTypeSize(treeNode);
    regNumber  targetReg  = treeNode->GetRegNum();
    var_types  targetType = treeNode->TypeGet();
    emitter*   emit       = GetEmitter();

    // Node's type must be int/native int, small integer types are not
    // supported and floating point types are handled by genCodeForBinary.
    assert(varTypeIsIntOrI(targetType));
    // dividend is in a register.
    assert(dividend->isUsedFromReg());

    genConsumeOperands(treeNode->AsOp());
    // dividend must be in RAX
    genCopyRegIfNeeded(dividend, REG_RAX);

    // zero or sign extend rax to rdx
    if (oper == GT_UMOD || oper == GT_UDIV ||
        (dividend->IsIntegralConst() && (dividend->AsIntConCommon()->IconValue() > 0)))
    {
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, REG_EDX);
    }
    else
    {
        emit->emitIns(INS_cdq, size);
        // the cdq instruction writes RDX, So clear the gcInfo for RDX
        gcInfo.gcMarkRegSetNpt(RBM_RDX);
    }

    // Perform the 'targetType' (64-bit or 32-bit) divide instruction
    instruction ins;
    if (oper == GT_UMOD || oper == GT_UDIV)
    {
        ins = INS_div;
    }
    else
    {
        ins = INS_idiv;
    }

    emit->emitInsBinary(ins, size, treeNode, divisor);

    // DIV/IDIV instructions always store the quotient in RAX and the remainder in RDX.
    // Move the result to the desired register, if necessary
    if (oper == GT_DIV || oper == GT_UDIV)
    {
        inst_Mov(targetType, targetReg, REG_RAX, /* canSkip */ true);
    }
    else
    {
        assert((oper == GT_MOD) || (oper == GT_UMOD));
        inst_Mov(targetType, targetReg, REG_RDX, /* canSkip */ true);
    }
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForBinary: Generate code for many binary arithmetic operators
//
// Arguments:
//    treeNode - The binary operation for which we are generating code.
//
// Return Value:
//    None.
//
// Notes:
//    Integer MUL and DIV variants have special constraints on x64 so are not handled here.
//    See the assert below for the operators that are handled.

void CodeGen::genCodeForBinary(GenTreeOp* treeNode)
{
#ifdef DEBUG
    bool isValidOper = treeNode->OperIs(GT_ADD, GT_SUB);
    if (varTypeIsFloating(treeNode->TypeGet()))
    {
        isValidOper |= treeNode->OperIs(GT_MUL, GT_DIV);
    }
    else
    {
        isValidOper |= treeNode->OperIs(GT_AND, GT_OR, GT_XOR);
#ifndef TARGET_64BIT
        isValidOper |= treeNode->OperIs(GT_ADD_LO, GT_ADD_HI, GT_SUB_LO, GT_SUB_HI);
#endif
    }
    assert(isValidOper);
#endif

    genConsumeOperands(treeNode);

    const genTreeOps oper       = treeNode->OperGet();
    regNumber        targetReg  = treeNode->GetRegNum();
    var_types        targetType = treeNode->TypeGet();
    emitter*         emit       = GetEmitter();

    GenTree* op1 = treeNode->gtGetOp1();
    GenTree* op2 = treeNode->gtGetOp2();

    // Commutative operations can mark op1 as contained or reg-optional to generate "op reg, memop/immed"
    if (!op1->isUsedFromReg())
    {
        assert(treeNode->OperIsCommutative());
        assert(op1->isMemoryOp() || op1->IsLocal() || op1->IsCnsNonZeroFltOrDbl() || op1->IsIntCnsFitsInI32() ||
               op1->IsRegOptional());

        op1 = treeNode->gtGetOp2();
        op2 = treeNode->gtGetOp1();
    }

    instruction ins = genGetInsForOper(treeNode->OperGet(), targetType);

    // The arithmetic node must be sitting in a register (since it's not contained)
    noway_assert(targetReg != REG_NA);

    regNumber op1reg = op1->isUsedFromReg() ? op1->GetRegNum() : REG_NA;
    regNumber op2reg = op2->isUsedFromReg() ? op2->GetRegNum() : REG_NA;

    if (varTypeIsFloating(treeNode->TypeGet()))
    {
        // floating-point addition, subtraction, multiplication, and division
        // all have RMW semantics if VEX support is not available

        bool isRMW = !compiler->canUseVexEncoding();
        inst_RV_RV_TT(ins, emitTypeSize(treeNode), targetReg, op1reg, op2, isRMW, INS_OPTS_NONE);

        genProduceReg(treeNode);
        return;
    }

    GenTree* dst;
    GenTree* src;

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

#ifdef DEBUG
        unsigned lclNum1 = (unsigned)-1;
        unsigned lclNum2 = (unsigned)-2;

        GenTree* op1Skip = op1->gtSkipReloadOrCopy();
        GenTree* op2Skip = op2->gtSkipReloadOrCopy();

        if (op1Skip->OperIsLocalRead())
        {
            lclNum1 = op1Skip->AsLclVarCommon()->GetLclNum();
        }
        if (op2Skip->OperIsLocalRead())
        {
            lclNum2 = op2Skip->AsLclVarCommon()->GetLclNum();
        }

        assert(GenTree::OperIsCommutative(oper) || (lclNum1 == lclNum2));
#endif

        dst = op2;
        src = op1;
    }
    // now we know there are 3 different operands so attempt to use LEA
    else if (oper == GT_ADD && !varTypeIsFloating(treeNode) && !treeNode->gtOverflowEx() // LEA does not set flags
             && (op2->isContainedIntOrIImmed() || op2->isUsedFromReg()) && !treeNode->gtSetFlags())
    {
        if (op2->isContainedIntOrIImmed())
        {
            emit->emitIns_R_AR(INS_lea, emitTypeSize(treeNode), targetReg, op1reg,
                               (int)op2->AsIntConCommon()->IconValue());
        }
        else
        {
            assert(op2reg != REG_NA);
            emit->emitIns_R_ARX(INS_lea, emitTypeSize(treeNode), targetReg, op1reg, op2reg, 1, 0);
        }
        genProduceReg(treeNode);
        return;
    }
    // dest, op1 and op2 registers are different:
    // reg3 = reg1 op reg2
    // We can implement this by issuing a mov:
    // reg3 = reg1
    // reg3 = reg3 op reg2
    else
    {
        var_types op1Type = op1->TypeGet();
        inst_Mov(op1Type, targetReg, op1reg, /* canSkip */ false);
        regSet.verifyRegUsed(targetReg);
        gcInfo.gcMarkRegPtrVal(targetReg, op1Type);
        dst = treeNode;
        src = op2;
    }

    // try to use an inc or dec
    if (oper == GT_ADD && !varTypeIsFloating(treeNode) && src->isContainedIntOrIImmed() && !treeNode->gtOverflowEx())
    {
        if (src->IsIntegralConst(1))
        {
            emit->emitIns_R(INS_inc, emitTypeSize(treeNode), targetReg);
            genProduceReg(treeNode);
            return;
        }
        else if (src->IsIntegralConst(-1))
        {
            emit->emitIns_R(INS_dec, emitTypeSize(treeNode), targetReg);
            genProduceReg(treeNode);
            return;
        }
    }
    regNumber r = emit->emitInsBinary(ins, emitTypeSize(treeNode), dst, src);
    noway_assert(r == targetReg);

    if (treeNode->gtOverflowEx())
    {
#if !defined(TARGET_64BIT)
        assert(oper == GT_ADD || oper == GT_SUB || oper == GT_ADD_HI || oper == GT_SUB_HI);
#else
        assert(oper == GT_ADD || oper == GT_SUB);
#endif
        genCheckOverflow(treeNode);
    }
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForMul: Generate code for a MUL operation.
//
// Arguments:
//    treeNode - the node to generate the code for
//
void CodeGen::genCodeForMul(GenTreeOp* treeNode)
{
    assert(treeNode->OperIs(GT_MUL));

    regNumber targetReg  = treeNode->GetRegNum();
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = GetEmitter();

    // Node's type must be int or long (only on x64), small integer types are not
    // supported and floating point types are handled by genCodeForBinary.
    assert(varTypeIsIntOrI(targetType));

    instruction ins;
    emitAttr    size                  = emitTypeSize(treeNode);
    bool        isUnsignedMultiply    = ((treeNode->gtFlags & GTF_UNSIGNED) != 0);
    bool        requiresOverflowCheck = treeNode->gtOverflowEx();

    GenTree* op1 = treeNode->gtGetOp1();
    GenTree* op2 = treeNode->gtGetOp2();

    // there are 3 forms of x64 multiply:
    // 1-op form with 128 result:  RDX:RAX = RAX * rm
    // 2-op form: reg *= rm
    // 3-op form: reg = rm * imm

    genConsumeOperands(treeNode);

    // This matches the 'mul' lowering in Lowering::SetMulOpCounts()
    //
    // immOp :: Only one operand can be an immediate
    // rmOp  :: Only one operand can be a memory op.
    // regOp :: A register op (especially the operand that matches 'targetReg')
    //          (can be nullptr when we have both a memory op and an immediate op)

    GenTree* immOp = nullptr;
    GenTree* rmOp  = op1;
    GenTree* regOp;

    if (op2->isContainedIntOrIImmed())
    {
        immOp = op2;
    }
    else if (op1->isContainedIntOrIImmed())
    {
        immOp = op1;
        rmOp  = op2;
    }

    if (immOp != nullptr)
    {
        ssize_t imm = immOp->AsIntConCommon()->IconValue();

        if (!requiresOverflowCheck && rmOp->isUsedFromReg() && ((imm == 3) || (imm == 5) || (imm == 9)))
        {
            // We will use the LEA instruction to perform this multiply
            // Note that an LEA with base=x, index=x and scale=(imm-1) computes x*imm when imm=3,5 or 9.
            unsigned int scale = (unsigned int)(imm - 1);
            GetEmitter()->emitIns_R_ARX(INS_lea, size, targetReg, rmOp->GetRegNum(), rmOp->GetRegNum(), scale, 0);
        }
        else
        {
            // use the 3-op form with immediate
            ins = GetEmitter()->inst3opImulForReg(targetReg);
            emit->emitInsBinary(ins, size, rmOp, immOp);
        }
    }
    else // we have no contained immediate operand
    {
        regOp = op1;
        rmOp  = op2;

        regNumber mulTargetReg = targetReg;
        if (isUnsignedMultiply && requiresOverflowCheck)
        {
            ins          = INS_mulEAX;
            mulTargetReg = REG_RAX;
        }
        else
        {
            ins = INS_imul;
        }

        // Set rmOp to the memory operand (if any)
        // or set regOp to the op2 when it has the matching target register for our multiply op
        //
        if (op1->isUsedFromMemory() || (op2->isUsedFromReg() && (op2->GetRegNum() == mulTargetReg)))
        {
            regOp = op2;
            rmOp  = op1;
        }
        assert(regOp->isUsedFromReg());

        // Setup targetReg when neither of the source operands was a matching register
        inst_Mov(targetType, mulTargetReg, regOp->GetRegNum(), /* canSkip */ true);

        emit->emitInsBinary(ins, size, treeNode, rmOp);

        // Move the result to the desired register, if necessary
        if (ins == INS_mulEAX)
        {
            inst_Mov(targetType, targetReg, REG_RAX, /* canSkip */ true);
        }
    }

    if (requiresOverflowCheck)
    {
        // Overflow checking is only used for non-floating point types
        noway_assert(!varTypeIsFloating(treeNode));

        genCheckOverflow(treeNode);
    }

    genProduceReg(treeNode);
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

    // This is a case of operand is in a single reg and needs to be
    // returned in multiple ABI return registers.
    regNumber opReg = src->GetRegNum();
    regNumber reg0  = retTypeDesc->GetABIReturnReg(0);
    regNumber reg1  = retTypeDesc->GetABIReturnReg(1);

    assert((reg0 != REG_NA) && (reg1 != REG_NA) && (opReg != REG_NA));

    const bool srcIsFloatReg = genIsValidFloatReg(opReg);
    const bool dstIsFloatReg = genIsValidFloatReg(reg0);
    assert(srcIsFloatReg);

#ifdef TARGET_AMD64
    assert(src->TypeIs(TYP_SIMD16));
    assert(srcIsFloatReg == dstIsFloatReg);
    assert(reg0 != reg1);

    // We can have one of three scenarios here.
    //
    // First, all three registers are different:
    //    opReg = xmm0
    //     reg1 = xmm1
    //     reg2 = xmm2
    // We can then generate two instructions:
    //    movaps  xmm1, xmm0    ; reg1[63:00] = opReg[ 63:00]
    //    movhlps xmm2, xmm0    ; reg2[63:00] = opReg[127:64]
    //
    // Second we have opReg and reg1 as the same register:
    //    opReg = xmm0
    //     reg1 = xmm0
    //     reg2 = xmm2
    // We can then generate one instruction:
    //    movhlps xmm2, xmm0    ; reg2[63:00] = opReg[127:64]
    //
    // Third we have opReg and reg2 as the same register:
    //    opReg = xmm0
    //     reg1 = xmm1
    //     reg2 = xmm0
    // We can then generate two instructions:
    //    movaps  xmm1, xmm0    ; reg1[63:00] = opReg[ 63:00]
    //    movhlps xmm0, xmm0    ; reg2[63:00] = opReg[127:64]

    // Move opReg into reg0, if not already there
    inst_Mov(TYP_SIMD16, reg0, opReg, /* canSkip */ true);

    // Move upper 64-bits of opReg into reg1
    GetEmitter()->emitIns_SIMD_R_R_R(INS_movhlps, EA_16BYTE, reg1, reg1, opReg);
#else  // TARGET_X86
    assert(src->TypeIs(TYP_SIMD8));
    assert(srcIsFloatReg != dstIsFloatReg);
    assert((reg0 == REG_EAX) && (reg1 == REG_EDX));

    // reg0 = opReg[31:0]
    inst_Mov(TYP_INT, reg0, opReg, /* canSkip */ false);

    // reg1 = opRef[61:32]
    if (compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        inst_RV_TT_IV(INS_pextrd, EA_4BYTE, reg1, src, 1);
    }
    else
    {
        bool   isRMW       = !compiler->canUseVexEncoding();
        int8_t shuffleMask = 1; // we only need [61:32]->[31:0], the rest is not read.

        inst_RV_RV_TT_IV(INS_pshufd, EA_8BYTE, opReg, opReg, src, shuffleMask, isRMW);
        inst_Mov(TYP_INT, reg1, opReg, /* canSkip */ false);
    }
#endif // TARGET_X86
}

#endif // FEATURE_SIMD

#if defined(TARGET_X86)

//------------------------------------------------------------------------
// genFloatReturn: Generates code for float return statement for x86.
//
// Note: treeNode's and op1's registers are already consumed.
//
// Arguments:
//    treeNode - The GT_RETURN or GT_RETFILT tree node with float type.
//
// Return Value:
//    None
//
void CodeGen::genFloatReturn(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    assert(varTypeIsFloating(treeNode));

    GenTree* op1 = treeNode->gtGetOp1();
    // Spill the return value register from an XMM register to the stack, then load it on the x87 stack.
    // If it already has a home location, use that. Otherwise, we need a temp.
    if (genIsRegCandidateLocal(op1) && compiler->lvaGetDesc(op1->AsLclVarCommon())->lvOnFrame)
    {
        if (compiler->lvaGetDesc(op1->AsLclVarCommon())->GetRegNum() != REG_STK)
        {
            op1->gtFlags |= GTF_SPILL;
            inst_TT_RV(ins_Store(op1->gtType, compiler->isSIMDTypeLocalAligned(op1->AsLclVarCommon()->GetLclNum())),
                       emitTypeSize(op1->TypeGet()), op1, op1->GetRegNum());
        }
        // Now, load it to the fp stack.
        GetEmitter()->emitIns_S(INS_fld, emitTypeSize(op1), op1->AsLclVarCommon()->GetLclNum(), 0);
    }
    else
    {
        // Spill the value, which should be in a register, then load it to the fp stack.
        // TODO-X86-CQ: Deal with things that are already in memory (don't call genConsumeReg yet).
        op1->gtFlags |= GTF_SPILL;
        regSet.rsSpillTree(op1->GetRegNum(), op1);
        op1->gtFlags |= GTF_SPILLED;
        op1->gtFlags &= ~GTF_SPILL;

        TempDsc* t = regSet.rsUnspillInPlace(op1, op1->GetRegNum());
        inst_FS_ST(INS_fld, emitActualTypeSize(op1->gtType), t, 0);
        op1->gtFlags &= ~GTF_SPILLED;
        regSet.tmpRlsTemp(t);
    }
}
#endif // TARGET_X86

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_EQ/GT_NE/GT_LT/GT_LE/GT_GE/GT_GT/GT_TEST_EQ/GT_TEST_NE/GT_CMP node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForCompare(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_EQ, GT_NE, GT_LT, GT_LE, GT_GE, GT_GT, GT_TEST_EQ, GT_TEST_NE, GT_BITTEST_EQ, GT_BITTEST_NE,
                        GT_CMP, GT_TEST, GT_BT));

    // TODO-XArch-CQ: Check if we can use the currently set flags.
    // TODO-XArch-CQ: Check for the case where we can simply transfer the carry bit to a register
    //         (signed < or >= where targetReg != REG_NA)

    GenTree*  op1     = tree->gtOp1;
    var_types op1Type = op1->TypeGet();

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
// genCodeForJTrue: Produce code for a GT_JTRUE node.
//
// Arguments:
//    jtrue - the node
//
void CodeGen::genCodeForJTrue(GenTreeOp* jtrue)
{
    assert(compiler->compCurBB->KindIs(BBJ_COND));

    GenTree*  op  = jtrue->gtGetOp1();
    regNumber reg = genConsumeReg(op);
    inst_RV_RV(INS_test, reg, reg, genActualType(op));
    inst_JMP(EJ_jne, compiler->compCurBB->GetTrueTarget());

    // If we cannot fall into the false target, emit a jump to it
    BasicBlock* falseTarget = compiler->compCurBB->GetFalseTarget();
    if (!compiler->compCurBB->CanRemoveJumpToTarget(falseTarget, compiler))
    {
        inst_JMP(EJ_jmp, falseTarget);
    }
}

//------------------------------------------------------------------------
// JumpKindToCmov:
//   Convert an emitJumpKind to the corresponding cmov instruction.
//
// Arguments:
//    condition - the condition
//
// Returns:
//    A cmov instruction.
//
instruction CodeGen::JumpKindToCmov(emitJumpKind condition)
{
    static constexpr instruction s_table[EJ_COUNT] = {
        INS_none,  INS_none,  INS_cmovo,  INS_cmovno, INS_cmovb,  INS_cmovae, INS_cmove,  INS_cmovne, INS_cmovbe,
        INS_cmova, INS_cmovs, INS_cmovns, INS_cmovp,  INS_cmovnp, INS_cmovl,  INS_cmovge, INS_cmovle, INS_cmovg,
    };

    static_assert_no_msg(s_table[EJ_NONE] == INS_none);
    static_assert_no_msg(s_table[EJ_jmp] == INS_none);
    static_assert_no_msg(s_table[EJ_jo] == INS_cmovo);
    static_assert_no_msg(s_table[EJ_jno] == INS_cmovno);
    static_assert_no_msg(s_table[EJ_jb] == INS_cmovb);
    static_assert_no_msg(s_table[EJ_jae] == INS_cmovae);
    static_assert_no_msg(s_table[EJ_je] == INS_cmove);
    static_assert_no_msg(s_table[EJ_jne] == INS_cmovne);
    static_assert_no_msg(s_table[EJ_jbe] == INS_cmovbe);
    static_assert_no_msg(s_table[EJ_ja] == INS_cmova);
    static_assert_no_msg(s_table[EJ_js] == INS_cmovs);
    static_assert_no_msg(s_table[EJ_jns] == INS_cmovns);
    static_assert_no_msg(s_table[EJ_jp] == INS_cmovp);
    static_assert_no_msg(s_table[EJ_jnp] == INS_cmovnp);
    static_assert_no_msg(s_table[EJ_jl] == INS_cmovl);
    static_assert_no_msg(s_table[EJ_jge] == INS_cmovge);
    static_assert_no_msg(s_table[EJ_jle] == INS_cmovle);
    static_assert_no_msg(s_table[EJ_jg] == INS_cmovg);

    assert((condition >= EJ_NONE) && (condition < EJ_COUNT));
    return s_table[condition];
}

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_SELECT/GT_SELECTCC node.
//
// Arguments:
//    select - the node
//
void CodeGen::genCodeForSelect(GenTreeOp* select)
{
    assert(select->OperIs(GT_SELECT, GT_SELECTCC));

    if (select->OperIs(GT_SELECT))
    {
        genConsumeRegs(select->AsConditional()->gtCond);
    }

    genConsumeOperands(select);

    regNumber dstReg = select->GetRegNum();

    GenTree* trueVal  = select->gtOp1;
    GenTree* falseVal = select->gtOp2;

    GenCondition cc = GenCondition::NE;

    if (select->OperIs(GT_SELECT))
    {
        GenTree*  cond    = select->AsConditional()->gtCond;
        regNumber condReg = cond->GetRegNum();
        GetEmitter()->emitIns_R_R(INS_test, emitActualTypeSize(cond), condReg, condReg);
    }
    else
    {
        cc = select->AsOpCC()->gtCondition;
    }

    // The usual codegen will be
    // mov targetReg, falseValue
    // cmovne targetReg, trueValue
    //
    // However, if the 'true' operand was allocated the same register as the
    // target register then prefer to generate
    //
    // mov targetReg, trueValue
    // cmove targetReg, falseValue
    //
    // so the first mov is elided.
    //
    if (falseVal->isUsedFromReg() && (falseVal->GetRegNum() == dstReg))
    {
        std::swap(trueVal, falseVal);
        cc = GenCondition::Reverse(cc);
    }

    // If there is a conflict then swap the condition anyway. LSRA should have
    // ensured the other way around has no conflict.
    if ((trueVal->gtGetContainedRegMask() & genRegMask(dstReg)) != 0)
    {
        std::swap(trueVal, falseVal);
        cc = GenCondition::Reverse(cc);
    }

    GenConditionDesc desc = GenConditionDesc::Get(cc);

    // There may also be a conflict with the falseVal in case this is an AND
    // condition. Once again, after swapping there should be no conflict as
    // ensured by LSRA.
    if ((desc.oper == GT_AND) && (falseVal->gtGetContainedRegMask() & genRegMask(dstReg)) != 0)
    {
        std::swap(trueVal, falseVal);
        cc   = GenCondition::Reverse(cc);
        desc = GenConditionDesc::Get(cc);
    }

    inst_RV_TT(INS_mov, emitTypeSize(select), dstReg, falseVal);

    assert(!trueVal->isContained() || trueVal->isUsedFromMemory());
    assert((trueVal->gtGetContainedRegMask() & genRegMask(dstReg)) == 0);
    inst_RV_TT(JumpKindToCmov(desc.jumpKind1), emitTypeSize(select), dstReg, trueVal);

    if (desc.oper == GT_AND)
    {
        assert(falseVal->isUsedFromReg());
        assert((falseVal->gtGetContainedRegMask() & genRegMask(dstReg)) == 0);
        inst_RV_TT(JumpKindToCmov(emitter::emitReverseJumpKind(desc.jumpKind2)), emitTypeSize(select), dstReg,
                   falseVal);
    }
    else if (desc.oper == GT_OR)
    {
        assert(trueVal->isUsedFromReg());
        inst_RV_TT(JumpKindToCmov(desc.jumpKind2), emitTypeSize(select), dstReg, trueVal);
    }

    genProduceReg(select);
}

// clang-format off
const CodeGen::GenConditionDesc CodeGen::GenConditionDesc::map[32]
{
    { },        // NONE
    { },        // 1
    { EJ_jl  }, // SLT
    { EJ_jle }, // SLE
    { EJ_jge }, // SGE
    { EJ_jg  }, // SGT
    { EJ_js  }, // S
    { EJ_jns }, // NS

    { EJ_je  }, // EQ
    { EJ_jne }, // NE
    { EJ_jb  }, // ULT
    { EJ_jbe }, // ULE
    { EJ_jae }, // UGE
    { EJ_ja  }, // UGT
    { EJ_jb  }, // C
    { EJ_jae }, // NC

    // Floating point compare instructions (UCOMISS, UCOMISD etc.) set the condition flags as follows:
    //    ZF PF CF  Meaning
    //   ---------------------
    //    1  1  1   Unordered
    //    0  0  0   Greater
    //    0  0  1   Less Than
    //    1  0  0   Equal
    //
    // Since ZF and CF are also set when the result is unordered, in some cases we first need to check
    // PF before checking ZF/CF. In general, ordered conditions will result in a jump only if PF is not
    // set and unordered conditions will result in a jump only if PF is set.

    { EJ_jnp, GT_AND, EJ_je  }, // FEQ
    { EJ_jne                 }, // FNE
    { EJ_jnp, GT_AND, EJ_jb  }, // FLT
    { EJ_jnp, GT_AND, EJ_jbe }, // FLE
    { EJ_jae                 }, // FGE
    { EJ_ja                  }, // FGT
    { EJ_jo                  }, // O
    { EJ_jno                 }, // NO

    { EJ_je                }, // FEQU
    { EJ_jp, GT_OR, EJ_jne }, // FNEU
    { EJ_jb                }, // FLTU
    { EJ_jbe               }, // FLEU
    { EJ_jp, GT_OR, EJ_jae }, // FGEU
    { EJ_jp, GT_OR, EJ_ja  }, // FGTU
    { EJ_jp                }, // P
    { EJ_jnp               }, // NP
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
    assert(genIsValidIntReg(dstReg) && isByteReg(dstReg));

    const GenConditionDesc& desc = GenConditionDesc::Get(condition);

    inst_SET(desc.jumpKind1, dstReg);

    if (desc.oper != GT_NONE)
    {
        BasicBlock* labelNext = genCreateTempLabel();
        inst_JMP((desc.oper == GT_OR) ? desc.jumpKind1 : emitter::emitReverseJumpKind(desc.jumpKind1), labelNext);
        inst_SET(desc.jumpKind2, dstReg);
        genDefineTempLabel(labelNext);
    }

    if (!varTypeIsByte(type))
    {
        GetEmitter()->emitIns_Mov(INS_movzx, EA_1BYTE, dstReg, dstReg, /* canSkip */ false);
    }
}

//------------------------------------------------------------------------
// inst_JMP: Generate a jump instruction.
//
void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock, bool isRemovableJmpCandidate)
{
#if !FEATURE_FIXED_OUT_ARGS
    // On the x86 we are pushing (and changing the stack level), but on x64 and other archs we have
    // a fixed outgoing args area that we store into and we never change the stack level when calling methods.
    //
    // Thus only on x86 do we need to assert that the stack level at the target block matches the current stack level.
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef UNIX_X86_ABI
    // bbTgtStkDepth is a (pure) argument count (stack alignment padding should be excluded).
    assert((tgtBlock->bbTgtStkDepth * sizeof(int) == (genStackLevel - curNestedAlignment)) || isFramePointerUsed());
#else
    assert((tgtBlock->bbTgtStkDepth * sizeof(int) == genStackLevel) || isFramePointerUsed());
#endif
#endif // !FEATURE_FIXED_OUT_ARGS

    GetEmitter()->emitIns_J(emitter::emitJumpKindToIns(jmp), tgtBlock, 0, isRemovableJmpCandidate);
}

//------------------------------------------------------------------------
// genCodeForReturnTrap: Produce code for a GT_RETURNTRAP node.
//
// Arguments:
//    tree - the GT_RETURNTRAP node
//
void CodeGen::genCodeForReturnTrap(GenTreeOp* tree)
{
    assert(tree->OperGet() == GT_RETURNTRAP);

    // this is nothing but a conditional call to CORINFO_HELP_STOP_FOR_GC
    // based on the contents of 'data'

    GenTree* data = tree->gtOp1;
    genConsumeRegs(data);
    GenTreeIntCon cns = intForm(TYP_INT, 0);
    cns.SetContained();
    GetEmitter()->emitInsBinary(INS_cmp, emitTypeSize(TYP_INT), data, &cns);

    BasicBlock* skipLabel = genCreateTempLabel();

    inst_JMP(EJ_je, skipLabel);

    // emit the call to the EE-helper that stops for GC (or other reasons)
    regNumber tmpReg = tree->GetSingleTempReg(RBM_ALLINT);
    assert(genIsValidIntReg(tmpReg));

    genEmitHelperCall(CORINFO_HELP_STOP_FOR_GC, 0, EA_UNKNOWN, tmpReg);
    genDefineTempLabel(skipLabel);
}

/*****************************************************************************
 *
 * Generate code for a single node in the tree.
 * Preconditions: All operands have been evaluated
 *
 */
void CodeGen::genCodeForTreeNode(GenTree* treeNode)
{
    regNumber targetReg;
#if !defined(TARGET_64BIT)
    if (treeNode->TypeGet() == TYP_LONG)
    {
        // All long enregistered nodes will have been decomposed into their
        // constituent lo and hi nodes.
        targetReg = REG_NA;
    }
    else
#endif // !defined(TARGET_64BIT)
    {
        targetReg = treeNode->GetRegNum();
    }
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
        genCodeForReuseVal(treeNode);
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
#ifndef JIT32_GCENCODER
        case GT_START_NONGC:
            GetEmitter()->emitDisableGC();
            break;
#endif // !defined(JIT32_GCENCODER)

        case GT_START_PREEMPTGC:
            // Kill callee saves GC registers, and create a label
            // so that information gets propagated to the emitter.
            gcInfo.gcMarkRegSetNpt(RBM_INT_CALLEE_SAVED);
            genDefineTempLabel(genCreateTempLabel());
            break;

        case GT_PROF_HOOK:
#ifdef PROFILING_SUPPORTED
            // We should be seeing this only if profiler hook is needed
            noway_assert(compiler->compIsProfilerHookNeeded());

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
#ifdef TARGET_X86
            assert(!treeNode->IsIconHandle(GTF_ICON_TLS_HDL));
#endif // TARGET_X86
            FALLTHROUGH;

        case GT_CNS_DBL:
            genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
            break;

        case GT_CNS_VEC:
            genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
            break;

        case GT_NOT:
        case GT_NEG:
            genCodeForNegNot(treeNode);
            break;

        case GT_BSWAP:
        case GT_BSWAP16:
            genCodeForBswap(treeNode);
            break;

        case GT_DIV:
            if (varTypeIsFloating(treeNode->TypeGet()))
            {
                genCodeForBinary(treeNode->AsOp());
                break;
            }
            FALLTHROUGH;
        case GT_MOD:
        case GT_UMOD:
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
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_MUL:
            if (varTypeIsFloating(treeNode->TypeGet()))
            {
                genCodeForBinary(treeNode->AsOp());
                break;
            }
            genCodeForMul(treeNode->AsOp());
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
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

        case GT_LCL_ADDR:
            genCodeForLclAddr(treeNode->AsLclFld());
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

        case GT_INC_SATURATE:
            genCodeForIncSaturate(treeNode);
            break;

        case GT_MULHI:
#ifdef TARGET_X86
        case GT_MUL_LONG:
#endif
            genCodeForMulHi(treeNode->AsOp());
            break;

        case GT_INTRINSIC:
            genIntrinsic(treeNode->AsIntrinsic());
            break;

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            genHWIntrinsic(treeNode->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_CKFINITE:
            genCkfinite(treeNode);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_BITTEST_EQ:
        case GT_BITTEST_NE:
        case GT_CMP:
        case GT_TEST:
        case GT_BT:
            genConsumeOperands(treeNode->AsOp());
            genCodeForCompare(treeNode->AsOp());
            break;

        case GT_JTRUE:
            genCodeForJTrue(treeNode->AsOp());
            break;

        case GT_JCC:
            genCodeForJcc(treeNode->AsCC());
            break;

        case GT_SETCC:
            genCodeForSetcc(treeNode->AsCC());
            break;

        case GT_SELECT:
            genCodeForSelect(treeNode->AsConditional());
            break;

        case GT_SELECTCC:
            genCodeForSelect(treeNode->AsOp());
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

        case GT_FIELD_LIST:
            // Should always be marked contained.
            assert(!"LIST, FIELD_LIST nodes should always be marked contained.");
            break;

        case GT_SWAP:
            genCodeForSwap(treeNode->AsOp());
            break;

        case GT_PUTARG_STK:
            genPutArgStk(treeNode->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
            genPutArgReg(treeNode->AsOp());
            break;

        case GT_CALL:
            genCall(treeNode->AsCall());
            break;

        case GT_JMP:
            genJmpMethod(treeNode);
            break;

        case GT_LOCKADD:
            genCodeForLockAdd(treeNode->AsOp());
            break;

        case GT_XCHG:
        case GT_XADD:
        case GT_XORR:
        case GT_XAND:
            genLockedInstructions(treeNode->AsOp());
            break;

        case GT_MEMORYBARRIER:
        {
            CodeGen::BarrierKind barrierKind =
                treeNode->gtFlags & GTF_MEMORYBARRIER_LOAD ? BARRIER_LOAD_ONLY : BARRIER_FULL;

            instGen_MemoryBarrier(barrierKind);
            break;
        }

        case GT_CMPXCHG:
            genCodeForCmpXchg(treeNode->AsCmpXchg());
            break;

        case GT_RELOAD:
            // do nothing - reload is just a marker.
            // The parent node will call genConsumeReg on this which will trigger the unspill of this node's child
            // into the register specified in this node.
            break;

        case GT_NOP:
            break;

#ifdef SWIFT_SUPPORT
        case GT_SWIFT_ERROR:
            genCodeForSwiftErrorReg(treeNode);
            break;
#endif // SWIFT_SUPPORT

        case GT_KEEPALIVE:
            genConsumeRegs(treeNode->AsOp()->gtOp1);
            break;

        case GT_NO_OP:
            GetEmitter()->emitIns_Nop(1);
            break;

        case GT_BOUNDS_CHECK:
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

#if !defined(FEATURE_EH_FUNCLETS)
        case GT_END_LFIN:

            // Have to clear the ShadowSP of the nesting level which encloses the finally. Generates:
            //     mov dword ptr [ebp-0xC], 0  // for some slot of the ShadowSP local var

            size_t finallyNesting;
            finallyNesting = treeNode->AsVal()->gtVal1;
            noway_assert(treeNode->AsVal()->gtVal1 < compiler->compHndBBtabCount);
            noway_assert(finallyNesting < compiler->compHndBBtabCount);

            // The last slot is reserved for ICodeManager::FixContext(ppEndRegion)
            unsigned filterEndOffsetSlotOffs;
            PREFIX_ASSUME(compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) >
                          TARGET_POINTER_SIZE); // below doesn't underflow.
            filterEndOffsetSlotOffs =
                (unsigned)(compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) - TARGET_POINTER_SIZE);

            size_t curNestingSlotOffs;
            curNestingSlotOffs = filterEndOffsetSlotOffs - ((finallyNesting + 1) * TARGET_POINTER_SIZE);
            GetEmitter()->emitIns_S_I(INS_mov, EA_PTRSIZE, compiler->lvaShadowSPslotsVar, (unsigned)curNestingSlotOffs,
                                      0);
            break;
#endif // !FEATURE_EH_FUNCLETS

        case GT_PINVOKE_PROLOG:
            noway_assert(((gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & ~fullIntArgRegMask()) == 0);

#ifdef PSEUDORANDOM_NOP_INSERTION
            // the runtime side requires the codegen here to be consistent
            emit->emitDisableRandomNops();
#endif // PSEUDORANDOM_NOP_INSERTION
            break;

        case GT_LABEL:
            genPendingCallLabel = genCreateTempLabel();
            emit->emitIns_R_L(INS_lea, EA_PTR_DSP_RELOC, genPendingCallLabel, treeNode->GetRegNum());
            break;

        case GT_STORE_BLK:
            genCodeForStoreBlk(treeNode->AsBlk());
            break;

        case GT_JMPTABLE:
            genJumpTable(treeNode);
            break;

        case GT_SWITCH_TABLE:
            genTableBasedSwitch(treeNode);
            break;

#if !defined(TARGET_64BIT)
        case GT_LONG:
            assert(treeNode->isUsedFromReg());
            genConsumeRegs(treeNode);
            break;
#endif

        case GT_IL_OFFSET:
            // Do nothing; these nodes are simply markers for debug info.
            break;

        default:
        {
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, ArrLen(message), _TRUNCATE, "NYI: Unimplemented node type %s\n",
                        GenTree::OpName(treeNode->OperGet()));
            NYIRAW(message);
#endif
            assert(!"Unknown node in codegen");
        }
        break;
    }
}

#ifdef FEATURE_SIMD
//----------------------------------------------------------------------------------
// genMultiRegStoreToSIMDLocal: store multi-reg value to a single-reg SIMD local
//
// Arguments:
//    lclNode  -  GenTreeLclVar of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
void CodeGen::genMultiRegStoreToSIMDLocal(GenTreeLclVar* lclNode)
{
    assert(varTypeIsSIMD(lclNode));

    regNumber dst       = lclNode->GetRegNum();
    GenTree*  op1       = lclNode->gtGetOp1();
    GenTree*  actualOp1 = op1->gtSkipReloadOrCopy();
    unsigned  regCount  = actualOp1->GetMultiRegCount(compiler);
    assert(op1->IsMultiRegNode());
    genConsumeRegs(op1);

    // Right now the only enregistrable structs supported are SIMD types.
    // They are only returned in 1 or 2 registers - the 1 register case is
    // handled as a regular STORE_LCL_VAR.
    // This case is always a call (AsCall() will assert if it is not).
    GenTreeCall*          call        = actualOp1->AsCall();
    const ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
    assert(retTypeDesc->GetReturnRegCount() == MAX_RET_REG_COUNT);

    assert(regCount == 2);
    regNumber targetReg = lclNode->GetRegNum();

    regNumber reg0 = call->GetRegNumByIdx(0);
    regNumber reg1 = call->GetRegNumByIdx(1);

    if (op1->IsCopyOrReload())
    {
        // GT_COPY/GT_RELOAD will have valid reg for those positions
        // that need to be copied or reloaded.
        regNumber reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(0);
        if (reloadReg != REG_NA)
        {
            reg0 = reloadReg;
        }

        reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(1);
        if (reloadReg != REG_NA)
        {
            reg1 = reloadReg;
        }
    }

#ifdef UNIX_AMD64_ABI
    assert(varTypeIsFloating(retTypeDesc->GetReturnRegType(0)));
    assert(varTypeIsFloating(retTypeDesc->GetReturnRegType(1)));

    // This is a case where the two 8-bytes that comprise the operand are in
    // two different xmm registers and need to be assembled into a single
    // xmm register.

    if (targetReg != reg1)
    {
        GetEmitter()->emitIns_SIMD_R_R_R(INS_movlhps, EA_16BYTE, targetReg, reg0, reg1);
    }
    else
    {
        // We need two shuffles to achieve this
        // First:
        // targetReg[ 63:00] = reg1[63:0]
        // targetReg[127:64] = reg0[63:0]
        //
        // Second:
        // targetReg[ 63:00] = targetReg[127:64]
        // targetReg[127:64] = targetReg[ 63:00]
        //
        // Essentially copy low 8-bytes from reg0 to high 8-bytes of targetReg
        // and next swap low and high 8-bytes of targetReg to have them
        // rearranged in the right order.

        GetEmitter()->emitIns_SIMD_R_R_R(INS_movlhps, EA_16BYTE, targetReg, reg1, reg0);
        GetEmitter()->emitIns_SIMD_R_R_R_I(INS_shufpd, EA_16BYTE, targetReg, targetReg, reg1, 0x01);
    }
    genProduceReg(lclNode);
#elif defined(TARGET_X86)
    if (TargetOS::IsWindows)
    {
        assert(varTypeIsIntegral(retTypeDesc->GetReturnRegType(0)));
        assert(varTypeIsIntegral(retTypeDesc->GetReturnRegType(1)));
        assert(lclNode->TypeIs(TYP_SIMD8));

        // This is a case where a SIMD8 struct returned as [EAX, EDX]
        // and needs to be assembled into a single xmm register,
        // note we can't check reg0=EAX, reg1=EDX because they could be already moved.

        inst_Mov(TYP_FLOAT, targetReg, reg0, /* canSkip */ false);
        const emitAttr size = emitTypeSize(TYP_SIMD8);
        if (compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41))
        {
            GetEmitter()->emitIns_SIMD_R_R_R_I(INS_pinsrd, size, targetReg, targetReg, reg1, 1);
        }
        else
        {
            regNumber tempXmm = lclNode->GetSingleTempReg();
            assert(tempXmm != targetReg);
            inst_Mov(TYP_FLOAT, tempXmm, reg1, /* canSkip */ false);
            GetEmitter()->emitIns_SIMD_R_R_R(INS_punpckldq, size, targetReg, targetReg, tempXmm);
        }
        genProduceReg(lclNode);
    }
#elif defined(TARGET_AMD64)
    assert(!TargetOS::IsWindows || !"Multireg store to SIMD reg not supported on Windows x64");
#else
#error Unsupported or unset target architecture
#endif
}
#endif // FEATURE_SIMD

//------------------------------------------------------------------------
// genEstablishFramePointer: Set up the frame pointer by adding an offset to the stack pointer.
//
// Arguments:
//    delta - the offset to add to the current stack pointer to establish the frame pointer
//    reportUnwindData - true if establishing the frame pointer should be reported in the OS unwind data.
//
void CodeGen::genEstablishFramePointer(int delta, bool reportUnwindData)
{
    assert(compiler->compGeneratingProlog);

    if (delta == 0)
    {
        GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, /* canSkip */ false);
    }
    else
    {
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);
        // We don't update prolog scope info (there is no function to handle lea), but that is currently dead code
        // anyway.
    }

    if (reportUnwindData)
    {
        compiler->unwindSetFrameReg(REG_FPBASE, delta);
    }
}

//------------------------------------------------------------------------
// genAllocLclFrame: Probe the stack and allocate the local stack frame - subtract from SP.
//
// Arguments:
//      frameSize         - the size of the stack frame being allocated.
//      initReg           - register to use as a scratch register.
//      pInitRegZeroed    - OUT parameter. *pInitRegZeroed is set to 'false' if and only if
//                          this call sets 'initReg' to a non-zero value.
//      maskArgRegsLiveIn - incoming argument registers that are currently live.
//
// Return value:
//      None
//
void CodeGen::genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn)
{
    assert(compiler->compGeneratingProlog);

    if (frameSize == 0)
    {
        return;
    }

    const target_size_t pageSize = compiler->eeGetPageSize();

    if (frameSize == REGSIZE_BYTES)
    {
        // Frame size is the same as register size.
        GetEmitter()->emitIns_R(INS_push, EA_PTRSIZE, REG_EAX);
        compiler->unwindAllocStack(frameSize);
    }
    else if (frameSize < pageSize)
    {
        GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, frameSize);
        compiler->unwindAllocStack(frameSize);

        const unsigned lastProbedLocToFinalSp = frameSize;

        if (lastProbedLocToFinalSp + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize)
        {
            // We haven't probed almost a complete page. If the next action on the stack might subtract from SP
            // first, before touching the current SP, then we need to probe at the very bottom. This can
            // happen on x86, for example, when we copy an argument to the stack using a "SUB ESP; REP MOV"
            // strategy.
            GetEmitter()->emitIns_R_AR(INS_test, EA_4BYTE, REG_EAX, REG_SPBASE, 0);
        }
    }
    else
    {
#ifdef TARGET_X86
        int spOffset = -(int)frameSize;

        if (compiler->info.compPublishStubParam)
        {
            GetEmitter()->emitIns_R(INS_push, EA_PTRSIZE, REG_SECRET_STUB_PARAM);
            spOffset += REGSIZE_BYTES;
        }

        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_STACK_PROBE_HELPER_ARG, REG_SPBASE, spOffset);
        regSet.verifyRegUsed(REG_STACK_PROBE_HELPER_ARG);

        genEmitHelperCall(CORINFO_HELP_STACK_PROBE, 0, EA_UNKNOWN);

        if (compiler->info.compPublishStubParam)
        {
            GetEmitter()->emitIns_R(INS_pop, EA_PTRSIZE, REG_SECRET_STUB_PARAM);
            GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, frameSize);
        }
        else
        {
            GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_SPBASE, REG_STACK_PROBE_HELPER_ARG, /* canSkip */ false);
        }
#else  // !TARGET_X86
        static_assert_no_msg((RBM_STACK_PROBE_HELPER_ARG & (RBM_SECRET_STUB_PARAM | RBM_DEFAULT_HELPER_CALL_TARGET)) ==
                             RBM_NONE);

        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_STACK_PROBE_HELPER_ARG, REG_SPBASE, -(int)frameSize);
        regSet.verifyRegUsed(REG_STACK_PROBE_HELPER_ARG);

        genEmitHelperCall(CORINFO_HELP_STACK_PROBE, 0, EA_UNKNOWN);

        if (initReg == REG_DEFAULT_HELPER_CALL_TARGET)
        {
            *pInitRegZeroed = false;
        }

        static_assert_no_msg((RBM_STACK_PROBE_HELPER_TRASH & RBM_STACK_PROBE_HELPER_ARG) == RBM_NONE);

        GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_SPBASE, REG_STACK_PROBE_HELPER_ARG, /* canSkip */ false);
#endif // !TARGET_X86

        compiler->unwindAllocStack(frameSize);

        if (initReg == REG_STACK_PROBE_HELPER_ARG)
        {
            *pInitRegZeroed = false;
        }
    }
}

//------------------------------------------------------------------------
// genStackPointerConstantAdjustment: add a specified constant value to the stack pointer.
// No probe is done.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative or zero.
//    trackSpAdjustments      - x86 only: whether or not to track the SP adjustment
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerConstantAdjustment(ssize_t spDelta, bool trackSpAdjustments)
{
    assert(spDelta < 0);

    // We assert that the SP change is less than one page. If it's greater, you should have called a
    // function that does a probe, which will in turn call this function.
    assert((target_size_t)(-spDelta) <= compiler->eeGetPageSize());

#ifdef TARGET_AMD64
    // We always track the SP adjustment on X64.
    trackSpAdjustments = true;
#endif // TARGET_AMD64

    if (trackSpAdjustments)
    {
        inst_RV_IV(INS_sub, REG_SPBASE, (target_ssize_t)-spDelta, EA_PTRSIZE);
    }
    else
    {
        // For x86, some cases don't want to track the adjustment to SP.
        inst_RV_IV(INS_sub_hide, REG_SPBASE, (target_ssize_t)-spDelta, EA_PTRSIZE);
    }
}

//------------------------------------------------------------------------
// genStackPointerConstantAdjustmentWithProbe: add a specified constant value to the stack pointer,
// and probe the stack as appropriate. Should only be called as a helper for
// genStackPointerConstantAdjustmentLoopWithProbe.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative or zero. If zero, the probe happens,
//                              but the stack pointer doesn't move.
//    trackSpAdjustments      - x86 only: whether or not to track the SP adjustment
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerConstantAdjustmentWithProbe(ssize_t spDelta, bool trackSpAdjustments)
{
    GetEmitter()->emitIns_AR_R(INS_TEST, EA_4BYTE, REG_SPBASE, REG_SPBASE, 0);
    genStackPointerConstantAdjustment(spDelta, trackSpAdjustments);
}

//------------------------------------------------------------------------
// genStackPointerConstantAdjustmentLoopWithProbe: Add a specified constant value to the stack pointer,
// and probe the stack as appropriate. Generates one probe per page, up to the total amount required.
// This will generate a sequence of probes in-line. It is required for the case where we need to expose
// (not hide) the stack level adjustment. We can't use the dynamic loop in that case, because the total
// stack adjustment would not be visible to the emitter. It would be possible to use this version for
// multiple hidden constant stack level adjustments but we don't do that currently (we use the loop
// version in genStackPointerDynamicAdjustmentWithProbe instead).
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative.
//    trackSpAdjustments      - x86 only: whether or not to track the SP adjustment
//
// Return Value:
//    Offset in bytes from SP to last probed address.
//
target_ssize_t CodeGen::genStackPointerConstantAdjustmentLoopWithProbe(ssize_t spDelta, bool trackSpAdjustments)
{
    assert(spDelta < 0);

    const target_size_t pageSize = compiler->eeGetPageSize();

    ssize_t spRemainingDelta = spDelta;
    do
    {
        ssize_t spOneDelta = -(ssize_t)min((target_size_t)-spRemainingDelta, pageSize);
        genStackPointerConstantAdjustmentWithProbe(spOneDelta, trackSpAdjustments);
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

        GetEmitter()->emitIns_AR_R(INS_test, EA_PTRSIZE, REG_EAX, REG_SPBASE, 0);
        lastTouchDelta = 0;
    }

    return lastTouchDelta;
}

//------------------------------------------------------------------------
// genStackPointerDynamicAdjustmentWithProbe: add a register value to the stack pointer,
// and probe the stack as appropriate.
//
// We hide the ESP adjustment from the emitter.
//
// Arguments:
//    regSpDelta              - the register value to add to SP. The value in this register must be negative.
//                              This register might be trashed.
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerDynamicAdjustmentWithProbe(regNumber regSpDelta)
{
    assert(regSpDelta != REG_NA);

    // Tickle the pages to ensure that ESP is always valid and is
    // in sync with the "stack guard page".  Note that in the worst
    // case ESP is on the last byte of the guard page.  Thus you must
    // touch ESP-0 first not ESP-0x1000.
    //
    // Another subtlety is that you don't want ESP to be exactly on the
    // boundary of the guard page because PUSH is predecrement, thus
    // call setup would not touch the guard page but just beyond it.
    //
    // Note that we go through a few hoops so that ESP never points to
    // illegal pages at any time during the tickling process
    //
    //       add   regSpDelta, ESP          // reg now holds ultimate ESP
    //       jb    loop                     // result is smaller than original ESP (no wrap around)
    //       xor   regSpDelta, regSpDelta   // Overflow, pick lowest possible number
    //  loop:
    //       test  ESP, [ESP+0]             // tickle the page
    //       sub   ESP, eeGetPageSize()
    //       cmp   ESP, regSpDelta
    //       jae   loop
    //       mov   ESP, regSpDelta

    BasicBlock* loop = genCreateTempLabel();

    inst_RV_RV(INS_add, regSpDelta, REG_SPBASE, TYP_I_IMPL);
    inst_JMP(EJ_jb, loop);

    instGen_Set_Reg_To_Zero(EA_PTRSIZE, regSpDelta);

    genDefineTempLabel(loop);

    // Tickle the decremented value. Note that it must be done BEFORE the update of ESP since ESP might already
    // be on the guard page. It is OK to leave the final value of ESP on the guard page.
    GetEmitter()->emitIns_AR_R(INS_TEST, EA_4BYTE, REG_SPBASE, REG_SPBASE, 0);

    // Subtract a page from ESP and hide the adjustment.
    inst_RV_IV(INS_sub_hide, REG_SPBASE, compiler->eeGetPageSize(), EA_PTRSIZE);

    inst_RV_RV(INS_cmp, REG_SPBASE, regSpDelta, TYP_I_IMPL);
    inst_JMP(EJ_jae, loop);

    // Move the final value to ESP
    inst_Mov(TYP_I_IMPL, REG_SPBASE, regSpDelta, /* canSkip */ false);
}

//------------------------------------------------------------------------
// genCodeForMemmove: Perform an unrolled memmove. The idea that we can
//    ignore the fact that src and dst might overlap if we save the whole
//    src to temp regs in advance, e.g. for memmove(dst: rcx, src: rax, len: 120):
//
//       vmovdqu  ymm0, ymmword ptr[rax +  0]
//       vmovdqu  ymm1, ymmword ptr[rax + 32]
//       vmovdqu  ymm2, ymmword ptr[rax + 64]
//       vmovdqu  ymm3, ymmword ptr[rax + 88]
//       vmovdqu  ymmword ptr[rcx +  0], ymm0
//       vmovdqu  ymmword ptr[rcx + 32], ymm1
//       vmovdqu  ymmword ptr[rcx + 64], ymm2
//       vmovdqu  ymmword ptr[rcx + 88], ymm3
//
// Arguments:
//    tree - GenTreeBlk node
//
void CodeGen::genCodeForMemmove(GenTreeBlk* tree)
{
    // Not yet finished for x86
    assert(TARGET_POINTER_SIZE == 8);

    // TODO-CQ: Support addressing modes, for now we don't use them
    GenTreeIndir* srcIndir = tree->Data()->AsIndir();
    assert(srcIndir->isContained() && !srcIndir->Addr()->isContained());

    regNumber dst  = genConsumeReg(tree->Addr());
    regNumber src  = genConsumeReg(srcIndir->Addr());
    unsigned  size = tree->Size();

    const unsigned simdSize = compiler->roundDownSIMDSize(size);
    if ((size >= simdSize) && (simdSize > 0))
    {
        // Number of SIMD regs needed to save the whole src to regs.
        unsigned numberOfSimdRegs = tree->AvailableTempRegCount(RBM_ALLFLOAT);

        // Lowering takes care to only introduce this node such that we will always have enough
        // temporary SIMD registers to fully load the source and avoid any potential issues with overlap.
        assert(numberOfSimdRegs * simdSize >= size);

        // Pop all temp regs to a local array, currently, this impl is limited with LSRA's MaxInternalCount
        regNumber tempRegs[LinearScan::MaxInternalCount] = {};
        for (unsigned i = 0; i < numberOfSimdRegs; i++)
        {
            tempRegs[i] = tree->ExtractTempReg(RBM_ALLFLOAT);
        }

        auto emitSimdLoadStore = [&](bool load) {
            unsigned    offset      = 0;
            int         regIndex    = 0;
            instruction simdMov     = simdUnalignedMovIns();
            unsigned    curSimdSize = simdSize;
            do
            {
                assert(curSimdSize >= XMM_REGSIZE_BYTES);
                if (load)
                {
                    // vmovdqu  ymm, ymmword ptr[src + offset]
                    GetEmitter()->emitIns_R_AR(simdMov, EA_ATTR(curSimdSize), tempRegs[regIndex++], src, offset);
                }
                else
                {
                    // vmovdqu  ymmword ptr[dst + offset], ymm
                    GetEmitter()->emitIns_AR_R(simdMov, EA_ATTR(curSimdSize), tempRegs[regIndex++], dst, offset);
                }
                offset += curSimdSize;
                if (size == offset)
                {
                    break;
                }

                // Overlap with the previously processed data. We'll always use SIMD for simplicity
                assert(size > offset);
                unsigned remainder = size - offset;
                if (remainder < curSimdSize)
                {
                    // Switch to smaller SIMD size if necessary
                    curSimdSize = compiler->roundUpSIMDSize(remainder);
                    offset      = size - curSimdSize;
                }
            } while (true);
        };

        // load everything from SRC to temp regs
        emitSimdLoadStore(/* load */ true);
        // store them to DST
        emitSimdLoadStore(/* load */ false);
    }
    else
    {
        // Here we work with size 1..15 (x64)
        assert((size > 0) && (size < XMM_REGSIZE_BYTES));

        auto emitScalarLoadStore = [&](bool load, int size, regNumber tempReg, int offset) {
            var_types memType;
            switch (size)
            {
                case 1:
                    memType = TYP_UBYTE;
                    break;
                case 2:
                    memType = TYP_USHORT;
                    break;
                case 4:
                    memType = TYP_INT;
                    break;
                case 8:
                    memType = TYP_LONG;
                    break;
                default:
                    unreached();
            }

            if (load)
            {
                // mov  reg, qword ptr [src + offset]
                GetEmitter()->emitIns_R_AR(ins_Load(memType), emitTypeSize(memType), tempReg, src, offset);
            }
            else
            {
                // mov  qword ptr [dst + offset], reg
                GetEmitter()->emitIns_AR_R(ins_Store(memType), emitTypeSize(memType), tempReg, dst, offset);
            }
        };

        // Use overlapping loads/stores, e. g. for size == 9: "mov [dst], tmpReg1; mov [dst+1], tmpReg2".
        unsigned loadStoreSize = 1 << BitOperations::Log2(size);
        if (loadStoreSize == size)
        {
            regNumber tmpReg = tree->GetSingleTempReg(RBM_ALLINT);
            emitScalarLoadStore(/* load */ true, loadStoreSize, tmpReg, 0);
            emitScalarLoadStore(/* load */ false, loadStoreSize, tmpReg, 0);
        }
        else
        {
            assert(tree->AvailableTempRegCount() == 2);
            regNumber tmpReg1 = tree->ExtractTempReg(RBM_ALLINT);
            regNumber tmpReg2 = tree->ExtractTempReg(RBM_ALLINT);
            emitScalarLoadStore(/* load */ true, loadStoreSize, tmpReg1, 0);
            emitScalarLoadStore(/* load */ true, loadStoreSize, tmpReg2, size - loadStoreSize);
            emitScalarLoadStore(/* load */ false, loadStoreSize, tmpReg1, 0);
            emitScalarLoadStore(/* load */ false, loadStoreSize, tmpReg2, size - loadStoreSize);
        }
    }
}

//------------------------------------------------------------------------
// genLclHeap: Generate code for localloc.
//
// Arguments:
//      tree - the localloc tree to generate.
//
// Notes:
//      Note that for x86, we don't track ESP movements while generating the localloc code.
//      The ESP tracking is used to report stack pointer-relative GC info, which is not
//      interesting while doing the localloc construction. Also, for functions with localloc,
//      we have EBP frames, and EBP-relative locals, and ESP-relative accesses only for function
//      call arguments.
//
//      For x86, we store the ESP after the localloc is complete in the LocAllocSP
//      variable. This variable is implicitly reported to the VM in the GC info (its position
//      is defined by convention relative to other items), and is used by the GC to find the
//      "base" stack pointer in functions with localloc.
//
void CodeGen::genLclHeap(GenTree* tree)
{
    assert(tree->OperGet() == GT_LCLHEAP);
    assert(compiler->compLocallocUsed);

    GenTree* size = tree->AsOp()->gtOp1;
    noway_assert((genActualType(size->gtType) == TYP_INT) || (genActualType(size->gtType) == TYP_I_IMPL));

    regNumber      targetReg      = tree->GetRegNum();
    regNumber      regCnt         = REG_NA;
    var_types      type           = genActualType(size->gtType);
    emitAttr       easz           = emitTypeSize(type);
    BasicBlock*    endLabel       = nullptr;
    target_ssize_t lastTouchDelta = (target_ssize_t)-1;

#ifdef DEBUG
    genStackPointerCheck(compiler->opts.compStackCheckOnRet, compiler->lvaReturnSpCheck);
#endif

    noway_assert(isFramePointerUsed()); // localloc requires Frame Pointer to be established since SP changes
    noway_assert(genStackLevel == 0);   // Can't have anything on the stack

    target_size_t stackAdjustment     = 0;
    target_size_t locAllocStackOffset = 0;

    // compute the amount of memory to allocate to properly STACK_ALIGN.
    size_t amount = 0;
    if (size->IsCnsIntOrI() && size->isContained())
    {
        amount = size->AsIntCon()->gtIconVal;
        assert((amount > 0) && (amount <= UINT_MAX));

        // 'amount' is the total number of bytes to localloc to properly STACK_ALIGN
        amount = AlignUp(amount, STACK_ALIGN);
    }
    else
    {
        // The localloc requested memory size is non-constant.

        // Put the size value in targetReg. If it is zero, bail out by returning null in targetReg.
        genConsumeRegAndCopy(size, targetReg);
        endLabel = genCreateTempLabel();
        GetEmitter()->emitIns_R_R(INS_test, easz, targetReg, targetReg);
        inst_JMP(EJ_je, endLabel);

        // Compute the size of the block to allocate and perform alignment.
        // If compInitMem=true, we can reuse targetReg as regcnt,
        // since we don't need any internal registers.
        if (compiler->info.compInitMem)
        {
            assert(tree->AvailableTempRegCount() == 0);
            regCnt = targetReg;
        }
        else
        {
            regCnt = tree->GetSingleTempReg();

            // Above, we put the size in targetReg. Now, copy it to our new temp register if necessary.
            inst_Mov(size->TypeGet(), regCnt, targetReg, /* canSkip */ true);
        }

        // Round up the number of bytes to allocate to a STACK_ALIGN boundary. This is done
        // by code like:
        //      add reg, 15
        //      and reg, -16
        // However, in the initialized memory case, we need the count of STACK_ALIGN-sized
        // elements, not a byte count, after the alignment. So instead of the "and", which
        // becomes unnecessary, generate a shift, e.g.:
        //      add reg, 15
        //      shr reg, 4

        inst_RV_IV(INS_add, regCnt, STACK_ALIGN - 1, emitActualTypeSize(type));

        if (compiler->info.compInitMem)
        {
            // Convert the count from a count of bytes to a loop count. We will loop once per
            // stack alignment size, so each loop will zero 4 bytes on Windows/x86, and 16 bytes
            // on x64 and Linux/x86.
            //
            // Note that we zero a single reg-size word per iteration on x86, and 2 reg-size
            // words per iteration on x64. We will shift off all the stack alignment bits
            // added above, so there is no need for an 'and' instruction.

            // --- shr regCnt, 2 (or 4) ---
            inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_PTRSIZE, regCnt, STACK_ALIGN_SHIFT);
        }
        else
        {
            // Otherwise, mask off the low bits to align the byte count.
            inst_RV_IV(INS_AND, regCnt, ~(STACK_ALIGN - 1), emitActualTypeSize(type));
        }
    }

    bool initMemOrLargeAlloc; // Declaration must be separate from initialization to avoid clang compiler error.
    initMemOrLargeAlloc = compiler->info.compInitMem || (amount >= compiler->eeGetPageSize()); // must be >= not >

#if FEATURE_FIXED_OUT_ARGS
    // If we have an outgoing arg area then we must adjust the SP by popping off the
    // outgoing arg area. We will restore it right before we return from this method.
    //
    // Localloc returns stack space that aligned to STACK_ALIGN bytes. The following
    // are the cases that need to be handled:
    //   i) Method has out-going arg area.
    //      It is guaranteed that size of out-going arg area is STACK_ALIGN'ed (see fgMorphArgs).
    //      Therefore, we will pop off the out-going arg area from RSP before allocating the localloc space.
    //  ii) Method has no out-going arg area.
    //      Nothing to pop off from the stack.
    if (compiler->lvaOutgoingArgSpaceSize > 0)
    {
        assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) == 0); // This must be true for the stack to remain
                                                                        // aligned

        // If the localloc amount is a small enough constant, and we're not initializing the allocated
        // memory, then don't bother popping off the ougoing arg space first; just allocate the amount
        // of space needed by the allocation, and call the bottom part the new outgoing arg space.

        if ((amount > 0) && !initMemOrLargeAlloc)
        {
            lastTouchDelta =
                genStackPointerConstantAdjustmentLoopWithProbe(-(ssize_t)amount, /* trackSpAdjustments */ true);
            stackAdjustment     = 0;
            locAllocStackOffset = (target_size_t)compiler->lvaOutgoingArgSpaceSize;
            goto ALLOC_DONE;
        }

        if (size->IsCnsIntOrI() && size->isContained())
        {
            stackAdjustment     = 0;
            locAllocStackOffset = (target_size_t)compiler->lvaOutgoingArgSpaceSize;
        }
        else
        {
            inst_RV_IV(INS_add, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize, EA_PTRSIZE);
            stackAdjustment += (target_size_t)compiler->lvaOutgoingArgSpaceSize;
            locAllocStackOffset = stackAdjustment;
        }
    }
#endif

    if (size->IsCnsIntOrI() && size->isContained())
    {
        // We should reach here only for non-zero, constant size allocations.
        assert(amount > 0);
        assert((amount % STACK_ALIGN) == 0);

        // We should reach here only for non-zero, constant size allocations which we zero
        // via BLK explicitly, so just bump the stack pointer.
        if ((amount >= compiler->eeGetPageSize()) || (TARGET_POINTER_SIZE == 4))
        {
            regCnt = tree->GetSingleTempReg();
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, regCnt, -(ssize_t)amount);
            genStackPointerDynamicAdjustmentWithProbe(regCnt);
            // lastTouchDelta is dynamic, and can be up to a page. So if we have outgoing arg space,
            // we're going to assume the worst and probe.
        }
        else
        {
            // Since the size is less than a page, and we don't need to zero init memory, simply adjust ESP.
            // ESP might already be in the guard page, so we must touch it BEFORE the alloc, not after.
            lastTouchDelta = genStackPointerConstantAdjustmentLoopWithProbe(-(ssize_t)amount,
                                                                            /* trackSpAdjustments */ true);
        }
        goto ALLOC_DONE;
    }

    // We should not have any temp registers at this point.
    assert(tree->AvailableTempRegCount() == 0);

    if (compiler->info.compInitMem)
    {
        // At this point 'regCnt' is set to the number of loop iterations for this loop, if each
        // iteration zeros (and subtracts from the stack pointer) STACK_ALIGN bytes.
        // Since we have to zero out the allocated memory AND ensure that RSP is always valid
        // by tickling the pages, we will just push 0's on the stack.

        assert(genIsValidIntReg(regCnt));

        // Loop:
        BasicBlock* loop = genCreateTempLabel();
        genDefineTempLabel(loop);

        static_assert_no_msg((STACK_ALIGN % REGSIZE_BYTES) == 0);
        unsigned const count = (STACK_ALIGN / REGSIZE_BYTES);

        for (unsigned i = 0; i < count; i++)
        {
            inst_IV(INS_push_hide, 0); // --- push REG_SIZE bytes of 0
        }
        // Note that the stack must always be aligned to STACK_ALIGN bytes

        // Decrement the loop counter and loop if not done.
        inst_RV(INS_dec, regCnt, TYP_I_IMPL);
        inst_JMP(EJ_jne, loop);

        lastTouchDelta = 0;
    }
    else
    {
        // At this point 'regCnt' is set to the total number of bytes to localloc.
        // Negate this value before calling the function to adjust the stack (which
        // adds to ESP).

        inst_RV(INS_NEG, regCnt, TYP_I_IMPL);
        genStackPointerDynamicAdjustmentWithProbe(regCnt);

        // lastTouchDelta is dynamic, and can be up to a page. So if we have outgoing arg space,
        // we're going to assume the worst and probe.
    }

ALLOC_DONE:
    // Re-adjust SP to allocate out-going arg area. Note: this also requires probes, if we have
    // a very large stack adjustment! For simplicity, we use the same function used elsewhere,
    // which probes the current address before subtracting. We may end up probing multiple
    // times relatively "nearby".
    if (stackAdjustment > 0)
    {
        assert((stackAdjustment % STACK_ALIGN) == 0); // This must be true for the stack to remain aligned
        assert(lastTouchDelta >= -1);

        if ((lastTouchDelta == (target_ssize_t)-1) ||
            (stackAdjustment + (target_size_t)lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES >
             compiler->eeGetPageSize()))
        {
            genStackPointerConstantAdjustmentLoopWithProbe(-(ssize_t)stackAdjustment, /* trackSpAdjustments */ true);
        }
        else
        {
            genStackPointerConstantAdjustment(-(ssize_t)stackAdjustment, /* trackSpAdjustments */ true);
        }
    }

    // Return the stackalloc'ed address in result register.
    // TargetReg = RSP + locAllocStackOffset
    GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, targetReg, REG_SPBASE, (int)locAllocStackOffset);

    if (endLabel != nullptr)
    {
        genDefineTempLabel(endLabel);
    }

#ifdef JIT32_GCENCODER
    if (compiler->lvaLocAllocSPvar != BAD_VAR_NUM)
    {
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaLocAllocSPvar, 0);
    }
#endif // JIT32_GCENCODER

#ifdef DEBUG
    // Update local variable to reflect the new stack pointer.
    if (compiler->opts.compStackCheckOnRet)
    {
        assert(compiler->lvaReturnSpCheck != BAD_VAR_NUM);
        assert(compiler->lvaGetDesc(compiler->lvaReturnSpCheck)->lvDoNotEnregister);
        assert(compiler->lvaGetDesc(compiler->lvaReturnSpCheck)->lvOnFrame);
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnSpCheck, 0);
    }
#endif

    genProduceReg(tree);
}

void CodeGen::genCodeForStoreBlk(GenTreeBlk* storeBlkNode)
{
    assert(storeBlkNode->OperIs(GT_STORE_BLK));

    bool isCopyBlk = storeBlkNode->OperIsCopyBlkOp();

    switch (storeBlkNode->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindCpObjRepInstr:
        case GenTreeBlk::BlkOpKindCpObjUnroll:
#ifndef JIT32_GCENCODER
            assert(!storeBlkNode->gtBlkOpGcUnsafe);
#endif
            genCodeForCpObj(storeBlkNode->AsBlk());
            break;

        case GenTreeBlk::BlkOpKindLoop:
            assert(!isCopyBlk);
            genCodeForInitBlkLoop(storeBlkNode);
            break;

        case GenTreeBlk::BlkOpKindRepInstr:
#ifndef JIT32_GCENCODER
            assert(!storeBlkNode->gtBlkOpGcUnsafe);
#endif
            if (isCopyBlk)
            {
                genCodeForCpBlkRepMovs(storeBlkNode);
            }
            else
            {
                genCodeForInitBlkRepStos(storeBlkNode);
            }
            break;
        case GenTreeBlk::BlkOpKindUnrollMemmove:
        case GenTreeBlk::BlkOpKindUnroll:
            if (isCopyBlk)
            {
#ifndef JIT32_GCENCODER
                if (storeBlkNode->gtBlkOpGcUnsafe)
                {
                    GetEmitter()->emitDisableGC();
                }
#endif
                if (storeBlkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll)
                {
                    genCodeForCpBlkUnroll(storeBlkNode);
                }
                else
                {
                    assert(storeBlkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnrollMemmove);
                    genCodeForMemmove(storeBlkNode);
                }
#ifndef JIT32_GCENCODER
                if (storeBlkNode->gtBlkOpGcUnsafe)
                {
                    GetEmitter()->emitEnableGC();
                }
#endif
            }
            else
            {
#ifndef JIT32_GCENCODER
                assert(!storeBlkNode->gtBlkOpGcUnsafe);
#endif
                genCodeForInitBlkUnroll(storeBlkNode);
            }
            break;
        default:
            unreached();
    }
}

//
//------------------------------------------------------------------------
// genCodeForInitBlkRepStos: Generate code for InitBlk using rep stos.
//
// Arguments:
//    initBlkNode - The Block store for which we are generating code.
//
void CodeGen::genCodeForInitBlkRepStos(GenTreeBlk* initBlkNode)
{
    genConsumeBlockOp(initBlkNode, REG_RDI, REG_RAX, REG_RCX);
    instGen(INS_r_stosb);
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

    unsigned  dstLclNum         = BAD_VAR_NUM;
    regNumber dstAddrBaseReg    = REG_NA;
    regNumber dstAddrIndexReg   = REG_NA;
    unsigned  dstAddrIndexScale = 1;
    int       dstOffset         = 0;
    GenTree*  dstAddr           = node->Addr();

    if (!dstAddr->isContained())
    {
        dstAddrBaseReg = genConsumeReg(dstAddr);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        GenTreeAddrMode* addrMode = dstAddr->AsAddrMode();

        if (addrMode->HasBase())
        {
            dstAddrBaseReg = genConsumeReg(addrMode->Base());
        }

        if (addrMode->HasIndex())
        {
            dstAddrIndexReg   = genConsumeReg(addrMode->Index());
            dstAddrIndexScale = addrMode->GetScale();
        }

        dstOffset = addrMode->Offset();
    }
    else
    {
        assert(dstAddr->OperIs(GT_LCL_ADDR));
        dstLclNum = dstAddr->AsLclVarCommon()->GetLclNum();
        dstOffset = dstAddr->AsLclVarCommon()->GetLclOffs();
    }

    regNumber srcIntReg = REG_NA;
    GenTree*  src       = node->Data();

    if (src->OperIs(GT_INIT_VAL))
    {
        assert(src->isContained());
        src = src->AsUnOp()->gtGetOp1();
    }

    unsigned size = node->GetLayout()->GetSize();

    // An SSE mov that accesses data larger than 8 bytes may be implemented using
    // multiple memory accesses. Hence, the JIT must not use such stores when
    // INITBLK zeroes a struct that contains GC pointers and can be observed by
    // other threads (i.e. when dstAddr is not an address of a local).
    // For example, this can happen when initializing a struct field of an object.
    const bool canUse16BytesSimdMov = !node->IsOnHeapAndContainsReferences() && compiler->IsBaselineSimdIsaSupported();
    const bool willUseSimdMov       = canUse16BytesSimdMov && (size >= XMM_REGSIZE_BYTES);

    if (!src->isContained())
    {
        srcIntReg = genConsumeReg(src);
    }
    else
    {
        assert(willUseSimdMov);
        assert(size >= XMM_REGSIZE_BYTES);
    }

    emitter* emit = GetEmitter();

    assert(size <= INT32_MAX);
    assert(dstOffset < (INT32_MAX - static_cast<int>(size)));

#ifdef FEATURE_SIMD
    if (willUseSimdMov)
    {
        regNumber srcXmmReg = node->GetSingleTempReg(RBM_ALLFLOAT);
        unsigned  regSize   = compiler->roundDownSIMDSize(size);
        var_types loadType  = compiler->getSIMDTypeForSize(regSize);
        simd_t    vecCon;
        memset(&vecCon, (uint8_t)src->AsIntCon()->IconValue(), sizeof(simd_t));
        genSetRegToConst(srcXmmReg, loadType, &vecCon);

        instruction simdMov      = simdUnalignedMovIns();
        unsigned    bytesWritten = 0;

        auto emitSimdMovs = [&]() {
            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_R(simdMov, EA_ATTR(regSize), srcXmmReg, dstLclNum, dstOffset);
            }
            else
            {
                emit->emitIns_ARX_R(simdMov, EA_ATTR(regSize), srcXmmReg, dstAddrBaseReg, dstAddrIndexReg,
                                    dstAddrIndexScale, dstOffset);
            }
        };

        while (bytesWritten < size)
        {
            if (bytesWritten + regSize > size)
            {
                // We have a remainder that is smaller than regSize.
                break;
            }

            emitSimdMovs();
            dstOffset += regSize;
            bytesWritten += regSize;
        }

        size -= bytesWritten;

        // Handle the remainder by overlapping with previously processed data
        if ((size > 0) && (size < regSize) && (regSize >= XMM_REGSIZE_BYTES))
        {
            // Get optimal register size to cover the whole remainder (with overlapping)
            regSize = compiler->roundUpSIMDSize(size);

            // Rewind dstOffset so we can fit a vector for the while remainder
            dstOffset -= (regSize - size);
            emitSimdMovs();
            size = 0;
        }
    }
#endif // FEATURE_SIMD

    assert((srcIntReg != REG_NA) || (size == 0));

// Fill the remainder using normal stores.
#ifdef TARGET_AMD64
    unsigned regSize = REGSIZE_BYTES;

    while (regSize > size)
    {
        regSize /= 2;
    }

    for (; size > regSize; size -= regSize, dstOffset += regSize)
    {
        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(INS_mov, EA_ATTR(regSize), srcIntReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_ARX_R(INS_mov, EA_ATTR(regSize), srcIntReg, dstAddrBaseReg, dstAddrIndexReg,
                                dstAddrIndexScale, dstOffset);
        }
    }

    // Handle the non-SIMD remainder by overlapping with previously processed data if needed
    if (size > 0)
    {
        assert(size <= REGSIZE_BYTES);

        // Round up to the closest power of two, but make sure it's not larger
        // than the register we used for the main loop
        regSize = min(regSize, compiler->roundUpGPRSize(size));

        unsigned shiftBack = regSize - size;
        assert(shiftBack <= regSize);
        dstOffset -= shiftBack;

        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(INS_mov, EA_ATTR(regSize), srcIntReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_ARX_R(INS_mov, EA_ATTR(regSize), srcIntReg, dstAddrBaseReg, dstAddrIndexReg,
                                dstAddrIndexScale, dstOffset);
        }
    }
#else // TARGET_X86
    for (unsigned regSize = REGSIZE_BYTES; size > 0; size -= regSize, dstOffset += regSize)
    {
        while (regSize > size)
        {
            regSize /= 2;
        }
        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(INS_mov, EA_ATTR(regSize), srcIntReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_ARX_R(INS_mov, EA_ATTR(regSize), srcIntReg, dstAddrBaseReg, dstAddrIndexReg,
                                dstAddrIndexScale, dstOffset);
        }
    }
#endif
}

//------------------------------------------------------------------------
// genCodeForInitBlkLoop - Generate code for an InitBlk using an inlined for-loop.
//    It's needed for cases when size is too big to unroll and we're not allowed
//    to use memset call due to atomicity requirements.
//
// Arguments:
//    initBlkNode - the GT_STORE_BLK node
//
void CodeGen::genCodeForInitBlkLoop(GenTreeBlk* initBlkNode)
{
    GenTree* const dstNode  = initBlkNode->Addr();
    GenTree* const zeroNode = initBlkNode->Data();

    genConsumeReg(dstNode);
    genConsumeReg(zeroNode);

    const regNumber dstReg  = dstNode->GetRegNum();
    const regNumber zeroReg = zeroNode->GetRegNum();

    //  xor      zeroReg, zeroReg
    //  mov      qword ptr [dstReg], zeroReg
    //  mov      offsetReg, <block size>
    //.LOOP:
    //  mov      qword ptr [dstReg + offsetReg], zeroReg
    //  sub      offsetReg, 8
    //  jne      .LOOP

    const unsigned size = initBlkNode->GetLayout()->GetSize();
    assert((size >= TARGET_POINTER_SIZE) && ((size % TARGET_POINTER_SIZE) == 0));

    // The loop is reversed - it makes it smaller.
    // Although, we zero the first pointer before the loop (the loop doesn't zero it)
    // it works as a nullcheck, otherwise the first iteration would try to access
    // "null + potentially large offset" and hit AV.
    GetEmitter()->emitIns_AR_R(INS_mov, EA_PTRSIZE, zeroReg, dstReg, 0);
    if (size > TARGET_POINTER_SIZE)
    {
        // Extend liveness of dstReg in case if it gets killed by the store.
        gcInfo.gcMarkRegPtrVal(dstReg, dstNode->TypeGet());

        const regNumber offsetReg = initBlkNode->GetSingleTempReg();
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, offsetReg, size - TARGET_POINTER_SIZE);

        BasicBlock* loop = genCreateTempLabel();
        genDefineTempLabel(loop);

        GetEmitter()->emitIns_ARX_R(INS_mov, EA_PTRSIZE, zeroReg, dstReg, offsetReg, 1, 0);
        GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, offsetReg, TARGET_POINTER_SIZE);
        inst_JMP(EJ_jne, loop);

        gcInfo.gcMarkRegSetNpt(genRegMask(dstReg));
    }
}

#ifdef FEATURE_PUT_STRUCT_ARG_STK
// Generate code for a load from some address + offset
//   base: tree node which can be either a local or an indir
//   offset: distance from the "base" location from which to load
//
void CodeGen::genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset)
{
    if (base->OperIsLocalRead())
    {
        GetEmitter()->emitIns_R_S(ins, size, dst, base->AsLclVarCommon()->GetLclNum(),
                                  offset + base->AsLclVarCommon()->GetLclOffs());
    }
    else
    {
        GetEmitter()->emitIns_R_AR(ins, size, dst, base->AsIndir()->Addr()->GetRegNum(), offset);
    }
}
#endif // FEATURE_PUT_STRUCT_ARG_STK

//----------------------------------------------------------------------------------
// genCodeForCpBlkUnroll - Generate unrolled block copy code.
//
// Arguments:
//    node - the GT_STORE_BLK node to generate code for
//
void CodeGen::genCodeForCpBlkUnroll(GenTreeBlk* node)
{
    assert(node->OperIs(GT_STORE_BLK));

    unsigned  dstLclNum         = BAD_VAR_NUM;
    regNumber dstAddrBaseReg    = REG_NA;
    regNumber dstAddrIndexReg   = REG_NA;
    unsigned  dstAddrIndexScale = 1;
    int       dstOffset         = 0;
    GenTree*  dstAddr           = node->Addr();

    if (!dstAddr->isContained())
    {
        dstAddrBaseReg = genConsumeReg(dstAddr);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        GenTreeAddrMode* addrMode = dstAddr->AsAddrMode();

        if (addrMode->HasBase())
        {
            dstAddrBaseReg = genConsumeReg(addrMode->Base());
        }

        if (addrMode->HasIndex())
        {
            dstAddrIndexReg   = genConsumeReg(addrMode->Index());
            dstAddrIndexScale = addrMode->GetScale();
        }

        dstOffset = addrMode->Offset();
    }
    else
    {
        assert(dstAddr->OperIs(GT_LCL_ADDR));
        const GenTreeLclVarCommon* lclVar = dstAddr->AsLclVarCommon();
        dstLclNum                         = lclVar->GetLclNum();
        dstOffset                         = lclVar->GetLclOffs();
    }

    unsigned  srcLclNum         = BAD_VAR_NUM;
    regNumber srcAddrBaseReg    = REG_NA;
    regNumber srcAddrIndexReg   = REG_NA;
    unsigned  srcAddrIndexScale = 1;
    int       srcOffset         = 0;
    GenTree*  src               = node->Data();

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
            GenTreeAddrMode* addrMode = srcAddr->AsAddrMode();

            if (addrMode->HasBase())
            {
                srcAddrBaseReg = genConsumeReg(addrMode->Base());
            }

            if (addrMode->HasIndex())
            {
                srcAddrIndexReg   = genConsumeReg(addrMode->Index());
                srcAddrIndexScale = addrMode->GetScale();
            }

            srcOffset = addrMode->Offset();
        }
        else
        {
            assert(srcAddr->OperIs(GT_LCL_ADDR));
            srcLclNum = srcAddr->AsLclVarCommon()->GetLclNum();
            srcOffset = srcAddr->AsLclVarCommon()->GetLclOffs();
        }
    }

    emitter* emit = GetEmitter();
    unsigned size = node->GetLayout()->GetSize();

    assert(size <= INT32_MAX);
    assert(srcOffset < (INT32_MAX - static_cast<int>(size)));
    assert(dstOffset < (INT32_MAX - static_cast<int>(size)));

    // Get the largest SIMD register available if the size is large enough
    unsigned regSize = compiler->roundDownSIMDSize(size);

    if ((size >= regSize) && (regSize > 0))
    {
        regNumber tempReg = node->GetSingleTempReg(RBM_ALLFLOAT);

        instruction simdMov = simdUnalignedMovIns();

        auto emitSimdMovs = [&]() {
            if (srcLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_R_S(simdMov, EA_ATTR(regSize), tempReg, srcLclNum, srcOffset);
            }
            else
            {
                emit->emitIns_R_ARX(simdMov, EA_ATTR(regSize), tempReg, srcAddrBaseReg, srcAddrIndexReg,
                                    srcAddrIndexScale, srcOffset);
            }

            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_R(simdMov, EA_ATTR(regSize), tempReg, dstLclNum, dstOffset);
            }
            else
            {
                emit->emitIns_ARX_R(simdMov, EA_ATTR(regSize), tempReg, dstAddrBaseReg, dstAddrIndexReg,
                                    dstAddrIndexScale, dstOffset);
            }
        };

        while (size >= regSize)
        {
            emitSimdMovs();
            srcOffset += regSize;
            dstOffset += regSize;
            size -= regSize;
        }

        assert((size >= 0) && (size < regSize));

        // Handle the remainder by overlapping with previously processed data
        if ((size > 0) && (size < regSize))
        {
            assert(regSize >= XMM_REGSIZE_BYTES);

            if (isPow2(size) && (size <= REGSIZE_BYTES))
            {
                // For sizes like 1,2,4 and 8 (on AMD64) we delegate handling to normal load/stores
            }
            else
            {
                // Get optimal register size to cover the whole remainder (with overlapping)
                regSize = compiler->roundUpSIMDSize(size);

                // Rewind dstOffset so we can fit a vector for the while remainder
                srcOffset -= (regSize - size);
                dstOffset -= (regSize - size);
                emitSimdMovs();
                size = 0;
            }
        }
    }

    // Fill the remainder with normal loads/stores
    if (size > 0)
    {
        regNumber tempReg = node->GetSingleTempReg(RBM_ALLINT);

#ifdef TARGET_AMD64
        unsigned regSize = REGSIZE_BYTES;

        while (regSize > size)
        {
            regSize /= 2;
        }

        for (; size > regSize; size -= regSize, srcOffset += regSize, dstOffset += regSize)
        {
            if (srcLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_R_S(INS_mov, EA_ATTR(regSize), tempReg, srcLclNum, srcOffset);
            }
            else
            {
                emit->emitIns_R_ARX(INS_mov, EA_ATTR(regSize), tempReg, srcAddrBaseReg, srcAddrIndexReg,
                                    srcAddrIndexScale, srcOffset);
            }

            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_R(INS_mov, EA_ATTR(regSize), tempReg, dstLclNum, dstOffset);
            }
            else
            {
                emit->emitIns_ARX_R(INS_mov, EA_ATTR(regSize), tempReg, dstAddrBaseReg, dstAddrIndexReg,
                                    dstAddrIndexScale, dstOffset);
            }
        }

        // Handle the non-SIMD remainder by overlapping with previously processed data if needed
        if (size > 0)
        {
            assert(size <= REGSIZE_BYTES);

            // Round up to the closest power of two, but make sure it's not larger
            // than the register we used for the main loop
            regSize = min(regSize, compiler->roundUpGPRSize(size));

            unsigned shiftBack = regSize - size;
            assert(shiftBack <= regSize);

            srcOffset -= shiftBack;
            dstOffset -= shiftBack;

            if (srcLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_R_S(INS_mov, EA_ATTR(regSize), tempReg, srcLclNum, srcOffset);
            }
            else
            {
                emit->emitIns_R_ARX(INS_mov, EA_ATTR(regSize), tempReg, srcAddrBaseReg, srcAddrIndexReg,
                                    srcAddrIndexScale, srcOffset);
            }

            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_R(INS_mov, EA_ATTR(regSize), tempReg, dstLclNum, dstOffset);
            }
            else
            {
                emit->emitIns_ARX_R(INS_mov, EA_ATTR(regSize), tempReg, dstAddrBaseReg, dstAddrIndexReg,
                                    dstAddrIndexScale, dstOffset);
            }
        }
#else // TARGET_X86
        for (unsigned regSize = REGSIZE_BYTES; size > 0; size -= regSize, srcOffset += regSize, dstOffset += regSize)
        {
            while (regSize > size)
            {
                regSize /= 2;
            }

            if (srcLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_R_S(INS_mov, EA_ATTR(regSize), tempReg, srcLclNum, srcOffset);
            }
            else
            {
                emit->emitIns_R_ARX(INS_mov, EA_ATTR(regSize), tempReg, srcAddrBaseReg, srcAddrIndexReg,
                                    srcAddrIndexScale, srcOffset);
            }

            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_R(INS_mov, EA_ATTR(regSize), tempReg, dstLclNum, dstOffset);
            }
            else
            {
                emit->emitIns_ARX_R(INS_mov, EA_ATTR(regSize), tempReg, dstAddrBaseReg, dstAddrIndexReg,
                                    dstAddrIndexScale, dstOffset);
            }
        }
#endif
    }
}

//----------------------------------------------------------------------------------
// genCodeForCpBlkRepMovs - Generate code for CpBlk by using rep movs
//
// Arguments:
//    cpBlkNode - the GT_STORE_[BLK|OBJ|DYN_BLK]
//
// Preconditions:
//   The register assignments have been set appropriately.
//   This is validated by genConsumeBlockOp().
//
void CodeGen::genCodeForCpBlkRepMovs(GenTreeBlk* cpBlkNode)
{
    // Destination address goes in RDI, source address goes in RSE, and size goes in RCX.
    // genConsumeBlockOp takes care of this for us.
    genConsumeBlockOp(cpBlkNode, REG_RDI, REG_RSI, REG_RCX);
    instGen(INS_r_movsb);
}

#ifdef FEATURE_PUT_STRUCT_ARG_STK
//------------------------------------------------------------------------
// CodeGen::genMove8IfNeeded: Conditionally move 8 bytes of a struct to the argument area
//
// Arguments:
//    size       - The size of bytes remaining to be moved
//    longTmpReg - The tmp register to be used for the long value
//    src        - The source struct node (LCL/OBJ)
//    offset     - The current offset being copied
//
// Return Value:
//    Returns the number of bytes moved (8 or 0).
//
// Notes:
//    This is used in the PutArgStkKindUnroll case, to move any bytes that are
//    not an even multiple of 16.
//    On x86, longTmpReg must be an xmm reg; on x64 it must be an integer register.
//    This is checked by genStoreRegToStackArg.
//
unsigned CodeGen::genMove8IfNeeded(unsigned size, regNumber longTmpReg, GenTree* src, unsigned offset)
{
#ifdef TARGET_X86
    instruction longMovIns = INS_movq;
#else  // !TARGET_X86
    instruction longMovIns = INS_mov;
#endif // !TARGET_X86
    if ((size & 8) != 0)
    {
        genCodeForLoadOffset(longMovIns, EA_8BYTE, longTmpReg, src, offset);
        genStoreRegToStackArg(TYP_LONG, longTmpReg, offset);
        return 8;
    }
    return 0;
}

//------------------------------------------------------------------------
// CodeGen::genMove4IfNeeded: Conditionally move 4 bytes of a struct to the argument area
//
// Arguments:
//    size      - The size of bytes remaining to be moved
//    intTmpReg - The tmp register to be used for the long value
//    src       - The source struct node (LCL/OBJ)
//    offset    - The current offset being copied
//
// Return Value:
//    Returns the number of bytes moved (4 or 0).
//
// Notes:
//    This is used in the PutArgStkKindUnroll case, to move any bytes that are
//    not an even multiple of 16.
//    intTmpReg must be an integer register.
//    This is checked by genStoreRegToStackArg.
//
unsigned CodeGen::genMove4IfNeeded(unsigned size, regNumber intTmpReg, GenTree* src, unsigned offset)
{
    if ((size & 4) != 0)
    {
        genCodeForLoadOffset(INS_mov, EA_4BYTE, intTmpReg, src, offset);
        genStoreRegToStackArg(TYP_INT, intTmpReg, offset);
        return 4;
    }
    return 0;
}

//------------------------------------------------------------------------
// CodeGen::genMove2IfNeeded: Conditionally move 2 bytes of a struct to the argument area
//
// Arguments:
//    size      - The size of bytes remaining to be moved
//    intTmpReg - The tmp register to be used for the long value
//    src       - The source struct node (LCL/OBJ)
//    offset    - The current offset being copied
//
// Return Value:
//    Returns the number of bytes moved (2 or 0).
//
// Notes:
//    This is used in the PutArgStkKindUnroll case, to move any bytes that are
//    not an even multiple of 16.
//    intTmpReg must be an integer register.
//    This is checked by genStoreRegToStackArg.
//
unsigned CodeGen::genMove2IfNeeded(unsigned size, regNumber intTmpReg, GenTree* src, unsigned offset)
{
    if ((size & 2) != 0)
    {
        genCodeForLoadOffset(INS_mov, EA_2BYTE, intTmpReg, src, offset);
        genStoreRegToStackArg(TYP_SHORT, intTmpReg, offset);
        return 2;
    }
    return 0;
}

//------------------------------------------------------------------------
// CodeGen::genMove1IfNeeded: Conditionally move 1 byte of a struct to the argument area
//
// Arguments:
//    size      - The size of bytes remaining to be moved
//    intTmpReg - The tmp register to be used for the long value
//    src       - The source struct node (LCL/OBJ)
//    offset    - The current offset being copied
//
// Return Value:
//    Returns the number of bytes moved (1 or 0).
//
// Notes:
//    This is used in the PutArgStkKindUnroll case, to move any bytes that are
//    not an even multiple of 16.
//    intTmpReg must be an integer register.
//    This is checked by genStoreRegToStackArg.
//
unsigned CodeGen::genMove1IfNeeded(unsigned size, regNumber intTmpReg, GenTree* src, unsigned offset)
{
    if ((size & 1) != 0)
    {
        genCodeForLoadOffset(INS_mov, EA_1BYTE, intTmpReg, src, offset);
        genStoreRegToStackArg(TYP_BYTE, intTmpReg, offset);
        return 1;
    }
    return 0;
}

//---------------------------------------------------------------------------------------------------------------//
// genStructPutArgUnroll: Generates code for passing a struct arg on stack by value using loop unrolling.
//
// Arguments:
//     putArgNode  - the PutArgStk tree.
//
// Notes:
//     m_stkArgVarNum must be set to the base var number, relative to which the by-val struct will be copied to the
//     stack.
//
// TODO-Amd64-Unix: Try to share code with copyblk.
//      Need refactoring of copyblk before it could be used for putarg_stk.
//      The difference for now is that a putarg_stk contains its children, while cpyblk does not.
//      This creates differences in code. After some significant refactoring it could be reused.
//
void CodeGen::genStructPutArgUnroll(GenTreePutArgStk* putArgNode)
{
    GenTree* src = putArgNode->Data();
    // We will never call this method for SIMD types, which are stored directly in genPutStructArgStk().
    assert(src->isContained() && src->TypeIs(TYP_STRUCT) && (src->OperIs(GT_BLK) || src->OperIsLocalRead()));

#ifdef TARGET_X86
    assert(!m_pushStkArg);
#endif

    if (src->OperIs(GT_BLK))
    {
        genConsumeReg(src->AsBlk()->Addr());
    }

    unsigned loadSize = putArgNode->GetArgLoadSize();
    assert(!src->GetLayout(compiler)->HasGCPtr() &&
           (loadSize <= compiler->getUnrollThreshold(Compiler::UnrollKind::Memcpy)));

    unsigned  offset     = 0;
    regNumber xmmTmpReg  = REG_NA;
    regNumber intTmpReg  = REG_NA;
    regNumber longTmpReg = REG_NA;

#ifdef TARGET_X86
    if (loadSize >= 8)
#else
    if (loadSize >= XMM_REGSIZE_BYTES)
#endif
    {
        xmmTmpReg = putArgNode->GetSingleTempReg(RBM_ALLFLOAT);
    }
    if ((loadSize % XMM_REGSIZE_BYTES) != 0)
    {
        intTmpReg = putArgNode->GetSingleTempReg(RBM_ALLINT);
    }

#ifdef TARGET_X86
    longTmpReg = xmmTmpReg;
#else
    longTmpReg = intTmpReg;
#endif

    // Let's use SSE2 to be able to do 16 byte at a time with loads and stores.
    size_t slots = loadSize / XMM_REGSIZE_BYTES;
    while (slots-- > 0)
    {
        // TODO: In the below code the load and store instructions are for 16 bytes, but the
        //       type is EA_8BYTE. The movdqa/u are 16 byte instructions, so it works, but
        //       this probably needs to be changed.

        // Load
        genCodeForLoadOffset(INS_movdqu, EA_16BYTE, xmmTmpReg, src, offset);
        // Store
        genStoreRegToStackArg(TYP_STRUCT, xmmTmpReg, offset);

        offset += XMM_REGSIZE_BYTES;
    }

    // Fill the remainder (15 bytes or less) if there's one.
    if ((loadSize % XMM_REGSIZE_BYTES) != 0)
    {
        offset += genMove8IfNeeded(loadSize, longTmpReg, src, offset);
        offset += genMove4IfNeeded(loadSize, intTmpReg, src, offset);
        offset += genMove2IfNeeded(loadSize, intTmpReg, src, offset);
        offset += genMove1IfNeeded(loadSize, intTmpReg, src, offset);
        assert(offset == loadSize);
    }
}

//------------------------------------------------------------------------
// genStructPutArgRepMovs: Generates code for passing a struct arg by value on stack using Rep Movs.
//
// Arguments:
//     putArgNode  - the PutArgStk tree.
//
// Preconditions:
//     m_stkArgVarNum must be set to the base var number, relative to which the by-val struct bits will go.
//
void CodeGen::genStructPutArgRepMovs(GenTreePutArgStk* putArgNode)
{
    GenTree* src = putArgNode->gtGetOp1();
    assert(src->TypeIs(TYP_STRUCT) && !src->GetLayout(compiler)->HasGCPtr());

    // Make sure we got the arguments of the cpblk operation in the right registers, and that
    // 'src' is contained as expected.
    assert(putArgNode->gtRsvdRegs == (RBM_RDI | RBM_RCX | RBM_RSI));
    assert(src->isContained());

    genConsumePutStructArgStk(putArgNode, REG_RDI, REG_RSI, REG_RCX);
    instGen(INS_r_movsb);
}

#ifdef TARGET_X86
//------------------------------------------------------------------------
// genStructPutArgPush: Generates code for passing a struct arg by value on stack using "push".
//
// Arguments:
//     putArgNode  - the PutArgStk tree.
//
// Notes:
//     Used (only) on x86 for:
//      - Structs 4, 8, or 12 bytes in size (less than XMM_REGSIZE_BYTES, multiple of TARGET_POINTER_SIZE).
//      - Local structs less than 16 bytes in size (it is ok to load "too much" from our stack frame).
//      - Structs that contain GC pointers - they are guaranteed to be sized correctly by the VM.
//
void CodeGen::genStructPutArgPush(GenTreePutArgStk* putArgNode)
{
    // On x86, any struct that contains GC references must be stored to the stack using `push` instructions so
    // that the emitter properly detects the need to update the method's GC information.
    //
    // Strictly speaking, it is only necessary to use "push" to store the GC references themselves, so for structs
    // with large numbers of consecutive non-GC-ref-typed fields, we may be able to improve the code size in the
    // future.
    assert(m_pushStkArg);

    GenTree*  src        = putArgNode->Data();
    regNumber srcAddrReg = REG_NA;
    unsigned  srcLclNum  = BAD_VAR_NUM;
    unsigned  srcLclOffs = BAD_LCL_OFFSET;
    if (src->OperIsLocalRead())
    {
        assert(src->isContained());
        srcLclNum  = src->AsLclVarCommon()->GetLclNum();
        srcLclOffs = src->AsLclVarCommon()->GetLclOffs();
    }
    else
    {
        srcAddrReg = genConsumeReg(src->AsBlk()->Addr());
    }

    ClassLayout*   layout   = src->GetLayout(compiler);
    const unsigned loadSize = putArgNode->GetArgLoadSize();
    assert(((loadSize < XMM_REGSIZE_BYTES) || layout->HasGCPtr()) && ((loadSize % TARGET_POINTER_SIZE) == 0));
    const unsigned numSlots = loadSize / TARGET_POINTER_SIZE;

    for (int i = numSlots - 1; i >= 0; --i)
    {
        emitAttr       slotAttr   = emitTypeSize(layout->GetGCPtrType(i));
        const unsigned byteOffset = i * TARGET_POINTER_SIZE;
        if (srcAddrReg != REG_NA)
        {
            GetEmitter()->emitIns_AR_R(INS_push, slotAttr, REG_NA, srcAddrReg, byteOffset);
        }
        else
        {
            GetEmitter()->emitIns_S(INS_push, slotAttr, srcLclNum, srcLclOffs + byteOffset);
        }

        AddStackLevel(TARGET_POINTER_SIZE);
    }
}
#endif // TARGET_X86

#ifndef TARGET_X86
//------------------------------------------------------------------------
// genStructPutArgPartialRepMovs: Generates code for passing a struct arg by value on stack using
//                                a mix of pointer-sized stores, "movsq" and "rep movsd".
//
// Arguments:
//     putArgNode  - the PutArgStk tree.
//
// Notes:
//     Used on non-x86 targets (Unix x64) for structs with GC pointers.
//
void CodeGen::genStructPutArgPartialRepMovs(GenTreePutArgStk* putArgNode)
{
    // Consume these registers.
    // They may now contain gc pointers (depending on their type; gcMarkRegPtrVal will "do the right thing").
    genConsumePutStructArgStk(putArgNode, REG_RDI, REG_RSI, REG_NA);

    GenTree*       src         = putArgNode->Data();
    ClassLayout*   layout      = src->GetLayout(compiler);
    const emitAttr srcAddrAttr = src->OperIsLocalRead() ? EA_PTRSIZE : EA_BYREF;

#if DEBUG
    unsigned numGCSlotsCopied = 0;
#endif // DEBUG

    assert(layout->HasGCPtr());
    const unsigned argSize = putArgNode->GetStackByteSize();
    assert(argSize % TARGET_POINTER_SIZE == 0);
    const unsigned numSlots = argSize / TARGET_POINTER_SIZE;

    // No need to disable GC the way COPYOBJ does. Here the refs are copied in atomic operations always.
    for (unsigned i = 0; i < numSlots;)
    {
        if (!layout->IsGCPtr(i))
        {
            // Let's see if we can use rep movsp (alias for movsd or movsq for 32 and 64 bits respectively)
            // instead of a sequence of movsp instructions to save cycles and code size.
            unsigned adjacentNonGCSlotCount = 0;
            do
            {
                adjacentNonGCSlotCount++;
                i++;
            } while ((i < numSlots) && !layout->IsGCPtr(i));

            // If we have a very small contiguous non-ref region, it's better just to
            // emit a sequence of movsp instructions
            if (adjacentNonGCSlotCount < CPOBJ_NONGC_SLOTS_LIMIT)
            {
                for (; adjacentNonGCSlotCount > 0; adjacentNonGCSlotCount--)
                {
                    instGen(INS_movsp);
                }
            }
            else
            {
                GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, REG_RCX, adjacentNonGCSlotCount);
                instGen(INS_r_movsp);
            }
        }
        else
        {
            // We have a GC (byref or ref) pointer
            // TODO-Amd64-Unix: Here a better solution (for code size and CQ) would be to use movsp instruction,
            // but the logic for emitting a GC info record is not available (it is internal for the emitter
            // only.) See emitGCVarLiveUpd function. If we could call it separately, we could do
            // instGen(INS_movsp); and emission of gc info.

            var_types memType = layout->GetGCPtrType(i);
            GetEmitter()->emitIns_R_AR(ins_Load(memType), emitTypeSize(memType), REG_RCX, REG_RSI, 0);
            genStoreRegToStackArg(memType, REG_RCX, i * TARGET_POINTER_SIZE);
#ifdef DEBUG
            numGCSlotsCopied++;
#endif // DEBUG

            i++;
            if (i < numSlots)
            {
                // Source for the copy operation.
                // If a LocalAddr, use EA_PTRSIZE - copy from stack.
                // If not a LocalAddr, use EA_BYREF - the source location is not on the stack.
                GetEmitter()->emitIns_R_I(INS_add, srcAddrAttr, REG_RSI, TARGET_POINTER_SIZE);

                // Always copying to the stack - outgoing arg area
                // (or the outgoing arg area of the caller for a tail call) - use EA_PTRSIZE.
                GetEmitter()->emitIns_R_I(INS_add, EA_PTRSIZE, REG_RDI, TARGET_POINTER_SIZE);
            }
        }
    }

    assert(numGCSlotsCopied == layout->GetGCPtrCount());
}
#endif // !TARGET_X86

//------------------------------------------------------------------------
// If any Vector3 args are on stack and they are not pass-by-ref, the upper 32bits
// must be cleared to zeroes. The native compiler doesn't clear the upper bits
// and there is no way to know if the caller is native or not. So, the upper
// 32 bits of Vector argument on stack are always cleared to zero.
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
void CodeGen::genClearStackVec3ArgUpperBits()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genClearStackVec3ArgUpperBits()\n");
    }
#endif

    assert(compiler->compGeneratingProlog);

    unsigned varNum = 0;

    for (unsigned varNum = 0; varNum < compiler->info.compArgsCount; varNum++)
    {
        const LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvIsParam);

        // Does var has simd12 type?
        if (varDsc->lvType != TYP_SIMD12)
        {
            continue;
        }

        if (!varDsc->lvIsRegArg)
        {
            // Clear the upper 32 bits by mov dword ptr [V_ARG_BASE+0xC], 0
            GetEmitter()->emitIns_S_I(ins_Store(TYP_INT), EA_4BYTE, varNum, genTypeSize(TYP_FLOAT) * 3, 0);
        }
        else
        {
            // Assume that for x64 linux, an argument is fully in registers
            // or fully on stack.
            regNumber argReg = varDsc->GetOtherArgReg();
            genSimd12UpperClear(argReg);
        }
    }
}
#endif // defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
#endif // FEATURE_PUT_STRUCT_ARG_STK

//
// genCodeForCpObj - Generate code for CpObj nodes to copy structs that have interleaved
//                   GC pointers.
//
// Arguments:
//    cpObjNode - the GT_STORE_BLK node
//
// Notes:
//    This will generate a sequence of movsp instructions for the cases of non-gc members.
//    Note that movsp is an alias for movsd on x86 and movsq on x64.
//    and calls to the BY_REF_ASSIGN helper otherwise.
//
// Preconditions:
//    The register assignments have been set appropriately.
//    This is validated by genConsumeBlockOp().
//
void CodeGen::genCodeForCpObj(GenTreeBlk* cpObjNode)
{
    // Make sure we got the arguments of the cpobj operation in the right registers
    GenTree*  dstAddr     = cpObjNode->Addr();
    GenTree*  source      = cpObjNode->Data();
    var_types srcAddrType = TYP_BYREF;
    bool      dstOnStack  = dstAddr->gtSkipReloadOrCopy()->OperIs(GT_LCL_ADDR);

    // If the GenTree node has data about GC pointers, this means we're dealing
    // with CpObj, so this requires special logic.
    assert(cpObjNode->GetLayout()->HasGCPtr());

    // MovSp (alias for movsq on x64 and movsd on x86) instruction is used for copying non-gcref fields
    // and it needs src = RSI and dst = RDI.
    // Either these registers must not contain lclVars, or they must be dying or marked for spill.
    // This is because these registers are incremented as we go through the struct.
    if (!source->IsLocal())
    {
        assert(source->gtOper == GT_IND);
        GenTree* srcAddr = source->gtGetOp1();
        srcAddrType      = srcAddr->TypeGet();

#ifdef DEBUG
        GenTree* actualSrcAddr    = srcAddr->gtSkipReloadOrCopy();
        GenTree* actualDstAddr    = dstAddr->gtSkipReloadOrCopy();
        unsigned srcLclVarNum     = BAD_VAR_NUM;
        unsigned dstLclVarNum     = BAD_VAR_NUM;
        bool     isSrcAddrLiveOut = false;
        bool     isDstAddrLiveOut = false;
        if (genIsRegCandidateLocal(actualSrcAddr))
        {
            srcLclVarNum     = actualSrcAddr->AsLclVarCommon()->GetLclNum();
            isSrcAddrLiveOut = ((actualSrcAddr->gtFlags & (GTF_VAR_DEATH | GTF_SPILL)) == 0);
        }
        if (genIsRegCandidateLocal(actualDstAddr))
        {
            dstLclVarNum     = actualDstAddr->AsLclVarCommon()->GetLclNum();
            isDstAddrLiveOut = ((actualDstAddr->gtFlags & (GTF_VAR_DEATH | GTF_SPILL)) == 0);
        }
        assert((actualSrcAddr->GetRegNum() != REG_RSI) || !isSrcAddrLiveOut ||
               ((srcLclVarNum == dstLclVarNum) && !isDstAddrLiveOut));
        assert((actualDstAddr->GetRegNum() != REG_RDI) || !isDstAddrLiveOut ||
               ((srcLclVarNum == dstLclVarNum) && !isSrcAddrLiveOut));
#endif // DEBUG
    }

    // Consume the operands and get them into the right registers.
    // They may now contain gc pointers (depending on their type; gcMarkRegPtrVal will "do the right thing").
    genConsumeBlockOp(cpObjNode, REG_RDI, REG_RSI, REG_NA);
    gcInfo.gcMarkRegPtrVal(REG_RSI, srcAddrType);
    gcInfo.gcMarkRegPtrVal(REG_RDI, dstAddr->TypeGet());

    unsigned slots = cpObjNode->GetLayout()->GetSlotCount();

    // If we can prove it's on the stack we don't need to use the write barrier.
    if (dstOnStack)
    {
        if (slots >= CPOBJ_NONGC_SLOTS_LIMIT)
        {
            // If the destination of the CpObj is on the stack, make sure we allocated
            // RCX to emit the movsp (alias for movsd or movsq for 32 and 64 bits respectively).
            assert((cpObjNode->gtRsvdRegs & RBM_RCX) != 0);

            GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, REG_RCX, slots);
            instGen(INS_r_movsp);
        }
        else
        {
            // For small structs, it's better to emit a sequence of movsp than to
            // emit a rep movsp instruction.
            while (slots > 0)
            {
                instGen(INS_movsp);
                slots--;
            }
        }
    }
    else
    {
        ClassLayout* layout     = cpObjNode->GetLayout();
        unsigned     gcPtrCount = layout->GetGCPtrCount();

        unsigned i = 0;
        while (i < slots)
        {
            if (!layout->IsGCPtr(i))
            {
                // Let's see if we can use rep movsp instead of a sequence of movsp instructions
                // to save cycles and code size.
                unsigned nonGcSlotCount = 0;

                do
                {
                    nonGcSlotCount++;
                    i++;
                } while ((i < slots) && !layout->IsGCPtr(i));

                // If we have a very small contiguous non-gc region, it's better just to
                // emit a sequence of movsp instructions
                if (nonGcSlotCount < CPOBJ_NONGC_SLOTS_LIMIT)
                {
                    while (nonGcSlotCount > 0)
                    {
                        instGen(INS_movsp);
                        nonGcSlotCount--;
                    }
                }
                else
                {
                    // Otherwise, we can save code-size and improve CQ by emitting
                    // rep movsp (alias for movsd/movsq for x86/x64)
                    assert((cpObjNode->gtRsvdRegs & RBM_RCX) != 0);

                    GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, REG_RCX, nonGcSlotCount);
                    instGen(INS_r_movsp);
                }
            }
            else
            {
                genEmitHelperCall(CORINFO_HELP_ASSIGN_BYREF, 0, EA_PTRSIZE);
                gcPtrCount--;
                i++;
            }
        }

        assert(gcPtrCount == 0);
    }

    // Clear the gcInfo for RSI and RDI.
    // While we normally update GC info prior to the last instruction that uses them,
    // these actually live into the helper call.
    gcInfo.gcMarkRegSetNpt(RBM_RSI);
    gcInfo.gcMarkRegSetNpt(RBM_RDI);
}

// generate code do a switch statement based on a table of ip-relative offsets
void CodeGen::genTableBasedSwitch(GenTree* treeNode)
{
    genConsumeOperands(treeNode->AsOp());
    regNumber idxReg  = treeNode->AsOp()->gtOp1->GetRegNum();
    regNumber baseReg = treeNode->AsOp()->gtOp2->GetRegNum();

    regNumber tmpReg = treeNode->GetSingleTempReg();

    // load the ip-relative offset (which is relative to start of fgFirstBB)
    GetEmitter()->emitIns_R_ARX(INS_mov, EA_4BYTE, baseReg, baseReg, idxReg, 4, 0);

    // add it to the absolute address of fgFirstBB
    GetEmitter()->emitIns_R_L(INS_lea, EA_PTR_DSP_RELOC, compiler->fgFirstBB, tmpReg);
    GetEmitter()->emitIns_R_R(INS_add, EA_PTRSIZE, baseReg, tmpReg);
    // jmp baseReg
    GetEmitter()->emitIns_R(INS_i_jmp, emitTypeSize(TYP_I_IMPL), baseReg);
}

// emits the table and an instruction to get the address of the first element
void CodeGen::genJumpTable(GenTree* treeNode)
{
    noway_assert(compiler->compCurBB->KindIs(BBJ_SWITCH));
    assert(treeNode->OperGet() == GT_JMPTABLE);

    unsigned   jumpCount = compiler->compCurBB->GetSwitchTargets()->bbsCount;
    FlowEdge** jumpTable = compiler->compCurBB->GetSwitchTargets()->bbsDstTab;
    unsigned   jmpTabOffs;
    unsigned   jmpTabBase;

    jmpTabBase = GetEmitter()->emitBBTableDataGenBeg(jumpCount, true);

    jmpTabOffs = 0;

    JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", compiler->compMethodID, jmpTabBase);

    for (unsigned i = 0; i < jumpCount; i++)
    {
        BasicBlock* target = (*jumpTable)->getDestinationBlock();
        jumpTable++;
        noway_assert(target->HasFlag(BBF_HAS_LABEL));

        JITDUMP("            DD      L_M%03u_" FMT_BB "\n", compiler->compMethodID, target->bbNum);

        GetEmitter()->emitDataGenData(i, target);
    };

    GetEmitter()->emitDataGenEnd();

    // Access to inline data is 'abstracted' by a special type of static member
    // (produced by eeFindJitDataOffs) which the emitter recognizes as being a reference
    // to constant data, not a real static field.
    GetEmitter()->emitIns_R_C(INS_lea, emitTypeSize(TYP_I_IMPL), treeNode->GetRegNum(),
                              compiler->eeFindJitDataOffs(jmpTabBase), 0);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForLockAdd: Generate code for a GT_LOCKADD node
//
// Arguments:
//    node - the GT_LOCKADD node
//
void CodeGen::genCodeForLockAdd(GenTreeOp* node)
{
    assert(node->OperIs(GT_LOCKADD));

    GenTree* addr = node->gtGetOp1();
    GenTree* data = node->gtGetOp2();
    emitAttr size = emitActualTypeSize(data->TypeGet());

    assert(addr->isUsedFromReg());
    assert(data->isUsedFromReg() || data->isContainedIntOrIImmed());
    assert((size == EA_4BYTE) || (size == EA_PTRSIZE));

    genConsumeOperands(node);
    instGen(INS_lock);

    if (data->isContainedIntOrIImmed())
    {
        int imm = static_cast<int>(data->AsIntCon()->IconValue());
        assert(imm == data->AsIntCon()->IconValue());
        if (imm == 1)
        {
            // inc [addr]
            GetEmitter()->emitIns_AR(INS_inc, size, addr->GetRegNum(), 0);
        }
        else if (imm == -1)
        {
            // dec [addr]
            GetEmitter()->emitIns_AR(INS_dec, size, addr->GetRegNum(), 0);
        }
        else
        {
            // add [addr], imm
            GetEmitter()->emitIns_I_AR(INS_add, size, imm, addr->GetRegNum(), 0);
        }
    }
    else
    {
        // add [addr], data
        GetEmitter()->emitIns_AR_R(INS_add, size, data->GetRegNum(), addr->GetRegNum(), 0);
    }
}

//------------------------------------------------------------------------
// genLockedInstructions: Generate code for a GT_XADD or GT_XCHG node.
//
// Arguments:
//    node - the GT_XADD/XCHG node
//
void CodeGen::genLockedInstructions(GenTreeOp* node)
{
    assert(node->OperIs(GT_XADD, GT_XCHG, GT_XORR, GT_XAND));
    assert(node->OperIs(GT_XCHG) || !varTypeIsSmall(node->TypeGet()));

    GenTree* addr = node->gtGetOp1();
    GenTree* data = node->gtGetOp2();
    emitAttr size = emitTypeSize(node->TypeGet());

    assert(addr->isUsedFromReg());
    assert(data->isUsedFromReg());
    assert((size <= EA_PTRSIZE) || (size == EA_GCREF));

    genConsumeOperands(node);

    if (node->OperIs(GT_XORR, GT_XAND))
    {
        const instruction ins = node->OperIs(GT_XORR) ? INS_or : INS_and;

        if (node->IsUnusedValue())
        {
            // If value is not used we can emit a short form:
            //
            //    lock
            //    or/and  dword ptr [addrReg], val
            //
            instGen(INS_lock);
            GetEmitter()->emitIns_AR_R(ins, size, data->GetRegNum(), addr->GetRegNum(), 0);
        }
        else
        {
            // When value is used (it's the original value of the memory location)
            // we fallback to cmpxchg-loop idiom.

            // for cmpxchg we need to keep the original value in RAX
            assert(node->GetRegNum() == REG_RAX);

            //    mov     RAX, dword ptr [addrReg]
            //.LOOP:
            //    mov     tmp, RAX
            //    or/and  tmp, val
            //    lock
            //    cmpxchg dword ptr [addrReg], tmp
            //    jne    .LOOP
            //    ret

            // Extend liveness of addr
            gcInfo.gcMarkRegPtrVal(addr->GetRegNum(), addr->TypeGet());

            const regNumber tmpReg = node->GetSingleTempReg();
            GetEmitter()->emitIns_R_AR(INS_mov, size, REG_RAX, addr->GetRegNum(), 0);
            BasicBlock* loop = genCreateTempLabel();
            genDefineTempLabel(loop);
            GetEmitter()->emitIns_Mov(INS_mov, size, tmpReg, REG_RAX, false);
            GetEmitter()->emitIns_R_R(ins, size, tmpReg, data->GetRegNum());
            instGen(INS_lock);
            GetEmitter()->emitIns_AR_R(INS_cmpxchg, size, tmpReg, addr->GetRegNum(), 0);
            inst_JMP(EJ_jne, loop);

            gcInfo.gcMarkRegSetNpt(genRegMask(addr->GetRegNum()));
            genProduceReg(node);
        }
        return;
    }

    // If the destination register is different from the data register then we need
    // to first move the data to the target register. Make sure we don't overwrite
    // the address, the register allocator should have taken care of this.
    assert((node->GetRegNum() != addr->GetRegNum()) || (node->GetRegNum() == data->GetRegNum()));
    GetEmitter()->emitIns_Mov(INS_mov, size, node->GetRegNum(), data->GetRegNum(), /* canSkip */ true);

    instruction ins = node->OperIs(GT_XADD) ? INS_xadd : INS_xchg;

    // XCHG has an implied lock prefix when the first operand is a memory operand.
    if (ins != INS_xchg)
    {
        instGen(INS_lock);
    }

    regNumber targetReg = node->GetRegNum();
    GetEmitter()->emitIns_AR_R(ins, size, targetReg, addr->GetRegNum(), 0);

    if (varTypeIsSmall(node->TypeGet()))
    {
        instruction mov = varTypeIsSigned(node->TypeGet()) ? INS_movsx : INS_movzx;
        GetEmitter()->emitIns_Mov(mov, size, targetReg, targetReg, /* canSkip */ false);
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genCodeForCmpXchg: Produce code for a GT_CMPXCHG node.
//
// Arguments:
//    tree - the GT_CMPXCHG node
//
void CodeGen::genCodeForCmpXchg(GenTreeCmpXchg* tree)
{
    assert(tree->OperIs(GT_CMPXCHG));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->GetRegNum();
    emitAttr  size       = emitTypeSize(tree->TypeGet());

    GenTree* location  = tree->Addr();      // arg1
    GenTree* value     = tree->Data();      // arg2
    GenTree* comparand = tree->Comparand(); // arg3

    assert(location->GetRegNum() != REG_NA && location->GetRegNum() != REG_RAX);
    assert(value->GetRegNum() != REG_NA && value->GetRegNum() != REG_RAX);

    genConsumeReg(location);
    genConsumeReg(value);
    genConsumeReg(comparand);

    // comparand goes to RAX;
    // Note that we must issue this move after the genConsumeRegs(), in case any of the above
    // have a GT_COPY from RAX.
    inst_Mov(comparand->TypeGet(), REG_RAX, comparand->GetRegNum(), /* canSkip */ true);

    // location is Rm
    instGen(INS_lock);

    GetEmitter()->emitIns_AR_R(INS_cmpxchg, size, value->GetRegNum(), location->GetRegNum(), 0);

    // Result is in RAX
    if (varTypeIsSmall(tree->TypeGet()))
    {
        instruction mov = varTypeIsSigned(tree->TypeGet()) ? INS_movsx : INS_movzx;
        GetEmitter()->emitIns_Mov(mov, size, targetReg, REG_RAX, /* canSkip */ false);
    }
    else
    {
        inst_Mov(targetType, targetReg, REG_RAX, /* canSkip */ true);
    }

    genProduceReg(tree);
}

// generate code for BoundsCheck nodes
void CodeGen::genRangeCheck(GenTree* oper)
{
    noway_assert(oper->OperIs(GT_BOUNDS_CHECK));
    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTree* arrIndex = bndsChk->GetIndex();
    GenTree* arrLen   = bndsChk->GetArrayLength();

    GenTree *    src1, *src2;
    emitJumpKind jmpKind;
    instruction  cmpKind;

    genConsumeRegs(arrIndex);
    genConsumeRegs(arrLen);

    if (arrIndex->IsIntegralConst(0) && arrLen->isUsedFromReg())
    {
        // arrIndex is 0 and arrLen is in a reg. In this case
        // we can generate
        //      test reg, reg
        // since arrLen is non-negative
        src1    = arrLen;
        src2    = arrLen;
        jmpKind = EJ_je;
        cmpKind = INS_test;
    }
    else if (arrIndex->isContainedIntOrIImmed())
    {
        // arrIndex is a contained constant.  In this case
        // we will generate one of the following
        //      cmp [mem], immed    (if arrLen is a memory op)
        //      cmp reg, immed      (if arrLen is in a reg)
        //
        // That is arrLen cannot be a contained immed.
        assert(!arrLen->isContainedIntOrIImmed());

        src1    = arrLen;
        src2    = arrIndex;
        jmpKind = EJ_jbe;
        cmpKind = INS_cmp;
    }
    else
    {
        // arrIndex could either be a contained memory op or a reg
        // In this case we will generate one of the following
        //      cmp  [mem], immed   (if arrLen is a constant)
        //      cmp  [mem], reg     (if arrLen is in a reg)
        //      cmp  reg, immed     (if arrIndex is in a reg)
        //      cmp  reg1, reg2     (if arrIndex is in reg1)
        //      cmp  reg, [mem]     (if arrLen is a memory op)
        //
        // That is only one of arrIndex or arrLen can be a memory op.
        assert(!arrIndex->isUsedFromMemory() || !arrLen->isUsedFromMemory());

        src1    = arrIndex;
        src2    = arrLen;
        jmpKind = EJ_jae;
        cmpKind = INS_cmp;
    }

    var_types bndsChkType = src2->TypeGet();
#if DEBUG
    // Bounds checks can only be 32 or 64 bit sized comparisons.
    assert(bndsChkType == TYP_INT || bndsChkType == TYP_LONG);

    // The type of the bounds check should always wide enough to compare against the index.
    assert(emitTypeSize(bndsChkType) >= emitTypeSize(src1->TypeGet()));
#endif // DEBUG

    GetEmitter()->emitInsBinary(cmpKind, emitTypeSize(bndsChkType), src1, src2);
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
    assert(tree->OperIs(GT_NULLCHECK));

    assert(tree->gtOp1->isUsedFromReg());
    regNumber reg = genConsumeReg(tree->gtOp1);
    GetEmitter()->emitIns_AR_R(INS_cmp, emitTypeSize(tree), reg, reg, 0);
}

instruction CodeGen::genGetInsForOper(genTreeOps oper, var_types type)
{
    instruction ins;

    // Operations on SIMD vectors shouldn't come this path
    assert(!varTypeIsSIMD(type));
    if (varTypeIsFloating(type))
    {
        return ins_MathOp(oper, type);
    }

    switch (oper)
    {
        case GT_ADD:
            ins = INS_add;
            break;
        case GT_AND:
            ins = INS_and;
            break;
        case GT_LSH:
            ins = INS_shl;
            break;
        case GT_MUL:
            ins = INS_imul;
            break;
        case GT_NEG:
            ins = INS_neg;
            break;
        case GT_NOT:
            ins = INS_not;
            break;
        case GT_OR:
            ins = INS_or;
            break;
        case GT_ROL:
            ins = INS_rol;
            break;
        case GT_ROR:
            ins = INS_ror;
            break;
        case GT_RSH:
            ins = INS_sar;
            break;
        case GT_RSZ:
            ins = INS_shr;
            break;
        case GT_SUB:
            ins = INS_sub;
            break;
        case GT_XOR:
            ins = INS_xor;
            break;
#if !defined(TARGET_64BIT)
        case GT_ADD_LO:
            ins = INS_add;
            break;
        case GT_ADD_HI:
            ins = INS_adc;
            break;
        case GT_SUB_LO:
            ins = INS_sub;
            break;
        case GT_SUB_HI:
            ins = INS_sbb;
            break;
        case GT_LSH_HI:
            ins = INS_shld;
            break;
        case GT_RSH_LO:
            ins = INS_shrd;
            break;
#endif // !defined(TARGET_64BIT)
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
//    b) The shift-by-amount in tree->AsOp()->gtOp2 is either a contained constant or
//       it's a register-allocated expression. If it is in a register that is
//       not RCX, it will be moved to RCX (so RCX better not be in use!).
//
void CodeGen::genCodeForShift(GenTree* tree)
{
    // Only the non-RMW case here.
    assert(tree->OperIsShiftOrRotate());
    assert(tree->AsOp()->gtOp1->isUsedFromReg());
    assert(tree->GetRegNum() != REG_NA);

    genConsumeOperands(tree->AsOp());

    var_types   targetType = tree->TypeGet();
    instruction ins        = genGetInsForOper(tree->OperGet(), targetType);

    GenTree*  operand    = tree->gtGetOp1();
    regNumber operandReg = operand->GetRegNum();

    GenTree* shiftBy = tree->gtGetOp2();

    if (shiftBy->isContainedIntOrIImmed())
    {
        emitAttr size = emitTypeSize(tree);

        bool mightOptimizeLsh = tree->OperIs(GT_LSH) && !tree->gtOverflowEx() && !tree->gtSetFlags();

        // Optimize "X<<1" to "lea [reg+reg]" or "add reg, reg"
        if (mightOptimizeLsh && shiftBy->IsIntegralConst(1))
        {
            if (tree->GetRegNum() == operandReg)
            {
                GetEmitter()->emitIns_R_R(INS_add, size, tree->GetRegNum(), operandReg);
            }
            else
            {
                GetEmitter()->emitIns_R_ARX(INS_lea, size, tree->GetRegNum(), operandReg, operandReg, 1, 0);
            }
        }
        // Optimize "X<<2" to "lea [reg*4]" - we only do this when the dst and src registers are different since it will
        // remove a 'mov'.
        else if (mightOptimizeLsh && shiftBy->IsIntegralConst(2) && tree->GetRegNum() != operandReg)
        {
            GetEmitter()->emitIns_R_ARX(INS_lea, size, tree->GetRegNum(), REG_NA, operandReg, 4, 0);
        }
        // Optimize "X<<3" to "lea [reg*8]" - we only do this when the dst and src registers are different since it will
        // remove a 'mov'.
        else if (mightOptimizeLsh && shiftBy->IsIntegralConst(3) && tree->GetRegNum() != operandReg)
        {
            GetEmitter()->emitIns_R_ARX(INS_lea, size, tree->GetRegNum(), REG_NA, operandReg, 8, 0);
        }
        else
        {
            int shiftByValue = (int)shiftBy->AsIntConCommon()->IconValue();

#if defined(TARGET_64BIT)
            // Try to emit rorx if BMI2 is available instead of mov+rol
            // it makes sense only for 64bit integers
            if ((genActualType(targetType) == TYP_LONG) && (tree->GetRegNum() != operandReg) &&
                compiler->compOpportunisticallyDependsOn(InstructionSet_BMI2) && tree->OperIs(GT_ROL, GT_ROR) &&
                (shiftByValue > 0) && (shiftByValue < 64))
            {
                const int value = tree->OperIs(GT_ROL) ? (64 - shiftByValue) : shiftByValue;
                GetEmitter()->emitIns_R_R_I(INS_rorx, size, tree->GetRegNum(), operandReg, value);
                genProduceReg(tree);
                return;
            }
#endif
            // First, move the operand to the destination register and
            // later on perform the shift in-place.
            // (LSRA will try to avoid this situation through preferencing.)
            inst_Mov(targetType, tree->GetRegNum(), operandReg, /* canSkip */ true);
            inst_RV_SH(ins, size, tree->GetRegNum(), shiftByValue);
        }
    }
#if defined(TARGET_64BIT)
    else if (tree->OperIsShift() && compiler->compOpportunisticallyDependsOn(InstructionSet_BMI2))
    {
        // Try to emit shlx, sarx, shrx if BMI2 is available instead of mov+shl, mov+sar, mov+shr.
        switch (tree->OperGet())
        {
            case GT_LSH:
                ins = INS_shlx;
                break;

            case GT_RSH:
                ins = INS_sarx;
                break;

            case GT_RSZ:
                ins = INS_shrx;
                break;

            default:
                unreached();
        }

        regNumber shiftByReg = shiftBy->GetRegNum();
        emitAttr  size       = emitTypeSize(tree);
        // The order of operandReg and shiftByReg are swapped to follow shlx, sarx and shrx encoding spec.
        GetEmitter()->emitIns_R_R_R(ins, size, tree->GetRegNum(), shiftByReg, operandReg);
    }
#endif
    else
    {
        // We must have the number of bits to shift stored in ECX, since we constrained this node to
        // sit in ECX. In case this didn't happen, LSRA expects the code generator to move it since it's a single
        // register destination requirement.
        genCopyRegIfNeeded(shiftBy, REG_RCX);

        // The operand to be shifted must not be in ECX
        noway_assert(operandReg != REG_RCX);

        inst_Mov(targetType, tree->GetRegNum(), operandReg, /* canSkip */ true);
        inst_RV(ins, tree->GetRegNum(), targetType);
    }

    genProduceReg(tree);
}

#ifdef TARGET_X86
//------------------------------------------------------------------------
// genCodeForShiftLong: Generates the code sequence for a GenTree node that
// represents a three operand bit shift or rotate operation (<<Hi, >>Lo).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//    b) The shift-by-amount in tree->AsOp()->gtOp2 is a contained constant
//
// TODO-X86-CQ: This only handles the case where the operand being shifted is in a register. We don't
// need sourceHi to be always in reg in case of GT_LSH_HI (because it could be moved from memory to
// targetReg if sourceHi is a memory operand). Similarly for GT_RSH_LO, sourceLo could be marked as
// contained memory-op. Even if not a memory-op, we could mark it as reg-optional.
//
void CodeGen::genCodeForShiftLong(GenTree* tree)
{
    // Only the non-RMW case here.
    genTreeOps oper = tree->OperGet();
    assert(oper == GT_LSH_HI || oper == GT_RSH_LO);

    GenTree* operand = tree->AsOp()->gtOp1;
    assert(operand->OperGet() == GT_LONG);
    assert(operand->AsOp()->gtOp1->isUsedFromReg());
    assert(operand->AsOp()->gtOp2->isUsedFromReg());

    GenTree* operandLo = operand->gtGetOp1();
    GenTree* operandHi = operand->gtGetOp2();

    regNumber regLo = operandLo->GetRegNum();
    regNumber regHi = operandHi->GetRegNum();

    genConsumeOperands(tree->AsOp());

    var_types   targetType = tree->TypeGet();
    instruction ins        = genGetInsForOper(oper, targetType);

    GenTree* shiftBy = tree->gtGetOp2();

    assert(shiftBy->isContainedIntOrIImmed());

    unsigned int count = (unsigned int)shiftBy->AsIntConCommon()->IconValue();

    if (oper == GT_LSH_HI)
    {
        regNumber tgtReg = tree->GetRegNum();
        assert(regLo != tgtReg);

        inst_Mov(targetType, tgtReg, regHi, /* canSkip */ true);
        inst_RV_RV_IV(ins, emitTypeSize(targetType), tree->GetRegNum(), regLo, count);
    }
    else
    {
        assert(oper == GT_RSH_LO);

        regNumber tgtReg = tree->GetRegNum();
        assert(regHi != tgtReg);

        inst_Mov(targetType, tgtReg, regLo, /* canSkip */ true);
        inst_RV_RV_IV(ins, emitTypeSize(targetType), tree->GetRegNum(), regHi, count);
    }

    genProduceReg(tree);
}
#endif

//------------------------------------------------------------------------
// genMapShiftInsToShiftByConstantIns: Given a general shift/rotate instruction,
// map it to the specific x86/x64 shift opcode for a shift/rotate by a constant.
// X86/x64 has a special encoding for shift/rotate-by-constant-1.
//
// Arguments:
//    ins: the base shift/rotate instruction
//    shiftByValue: the constant value by which we are shifting/rotating
//
instruction CodeGen::genMapShiftInsToShiftByConstantIns(instruction ins, int shiftByValue)
{
    assert(ins == INS_rcl || ins == INS_rcr || ins == INS_rol || ins == INS_ror || ins == INS_shl || ins == INS_shr ||
           ins == INS_sar);

    // Which format should we use?

    instruction shiftByConstantIns;

    if (shiftByValue == 1)
    {
        // Use the shift-by-one format.

        assert(INS_rcl + 1 == INS_rcl_1);
        assert(INS_rcr + 1 == INS_rcr_1);
        assert(INS_rol + 1 == INS_rol_1);
        assert(INS_ror + 1 == INS_ror_1);
        assert(INS_shl + 1 == INS_shl_1);
        assert(INS_shr + 1 == INS_shr_1);
        assert(INS_sar + 1 == INS_sar_1);

        shiftByConstantIns = (instruction)(ins + 1);
    }
    else
    {
        // Use the shift-by-NNN format.

        assert(INS_rcl + 2 == INS_rcl_N);
        assert(INS_rcr + 2 == INS_rcr_N);
        assert(INS_rol + 2 == INS_rol_N);
        assert(INS_ror + 2 == INS_ror_N);
        assert(INS_shl + 2 == INS_shl_N);
        assert(INS_shr + 2 == INS_shr_N);
        assert(INS_sar + 2 == INS_sar_N);

        shiftByConstantIns = (instruction)(ins + 2);
    }

    return shiftByConstantIns;
}

//------------------------------------------------------------------------
// genCodeForShiftRMW: Generates the code sequence for a GT_STOREIND GenTree node that
// represents a RMW bit shift or rotate operation (<<, >>, >>>, rol, ror), for example:
//      GT_STOREIND( AddressTree, GT_SHL( Ind ( AddressTree ), Operand ) )
//
// Arguments:
//    storeIndNode: the GT_STOREIND node.
//
void CodeGen::genCodeForShiftRMW(GenTreeStoreInd* storeInd)
{
    GenTree* data = storeInd->Data();

    assert(data->OperIsShift() || data->OperIsRotate());

    // This function only handles the RMW case.
    assert(data->AsOp()->gtOp1->isUsedFromMemory());
    assert(data->AsOp()->gtOp1->isIndir());
    assert(Lowering::IndirsAreEquivalent(data->AsOp()->gtOp1, storeInd));
    assert(data->GetRegNum() == REG_NA);

    var_types   targetType = data->TypeGet();
    genTreeOps  oper       = data->OperGet();
    instruction ins        = genGetInsForOper(oper, targetType);
    emitAttr    attr       = EA_ATTR(genTypeSize(targetType));

    GenTree* shiftBy = data->AsOp()->gtOp2;
    if (shiftBy->isContainedIntOrIImmed())
    {
        int shiftByValue = (int)shiftBy->AsIntConCommon()->IconValue();
        ins              = genMapShiftInsToShiftByConstantIns(ins, shiftByValue);
        if (shiftByValue == 1)
        {
            // There is no source in this case, as the shift by count is embedded in the instruction opcode itself.
            GetEmitter()->emitInsRMW(ins, attr, storeInd);
        }
        else
        {
            GetEmitter()->emitInsRMW(ins, attr, storeInd, shiftBy);
        }
    }
    else
    {
        // We must have the number of bits to shift stored in ECX, since we constrained this node to
        // sit in ECX. In case this didn't happen, LSRA expects the code generator to move it since it's a single
        // register destination requirement.
        genCopyRegIfNeeded(shiftBy, REG_RCX);

        // The shiftBy operand is implicit, so call the unary version of emitInsRMW.
        GetEmitter()->emitInsRMW(ins, attr, storeInd);
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

    var_types targetType = lclAddrNode->TypeGet();
    emitAttr  size       = emitTypeSize(targetType);
    regNumber targetReg  = lclAddrNode->GetRegNum();

    // Address of a local var.
    noway_assert((targetType == TYP_BYREF) || (targetType == TYP_I_IMPL));

    GetEmitter()->emitIns_R_S(INS_lea, size, targetReg, lclAddrNode->GetLclNum(), lclAddrNode->GetLclOffs());

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

    var_types targetType = tree->TypeGet();

#ifdef FEATURE_SIMD
    // Loading of TYP_SIMD12 (i.e. Vector3) field
    if (targetType == TYP_SIMD12)
    {
        genLoadLclTypeSimd12(tree);
        return;
    }
#endif

    regNumber targetReg = tree->GetRegNum();
    noway_assert(targetReg != REG_NA);

    noway_assert(targetType != TYP_STRUCT);

    emitAttr size   = emitTypeSize(targetType);
    unsigned offs   = tree->GetLclOffs();
    unsigned varNum = tree->GetLclNum();
    assert(varNum < compiler->lvaCount);

    instruction loadIns = tree->DontExtend() ? INS_mov : ins_Load(targetType);
    GetEmitter()->emitIns_R_S(loadIns, size, targetReg, varNum, offs);

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
    assert(tree->OperIs(GT_LCL_VAR));

    // lcl_vars are not defs
    assert((tree->gtFlags & GTF_VAR_DEF) == 0);

    LclVarDsc* varDsc         = compiler->lvaGetDesc(tree);
    bool       isRegCandidate = varDsc->lvIsRegCandidate();

    // If this is a register candidate that has been spilled, genConsumeReg() will
    // reload it at the point of use.  Otherwise, if it's not in a register, we load it here.

    if (!isRegCandidate && !tree->IsMultiReg() && !(tree->gtFlags & GTF_SPILLED))
    {
#if defined(FEATURE_SIMD) && defined(TARGET_X86)
        // Loading of TYP_SIMD12 (i.e. Vector3) variable
        if (tree->TypeGet() == TYP_SIMD12)
        {
            genLoadLclTypeSimd12(tree);
            return;
        }
#endif // defined(FEATURE_SIMD) && defined(TARGET_X86)

        var_types type = varDsc->GetRegisterType(tree);
        GetEmitter()->emitIns_R_S(ins_Load(type, compiler->isSIMDTypeLocalAligned(tree->GetLclNum())),
                                  emitTypeSize(type), tree->GetRegNum(), tree->GetLclNum(), 0);
        genProduceReg(tree);
    }
}

//------------------------------------------------------------------------
// genCodeForStoreLclFld: Produce code for a GT_STORE_LCL_FLD node.
//
// Arguments:
//    tree - the GT_STORE_LCL_FLD node
//
void CodeGen::genCodeForStoreLclFld(GenTreeLclFld* tree)
{
    assert(tree->OperIs(GT_STORE_LCL_FLD));

    var_types targetType = tree->TypeGet();
    noway_assert(targetType != TYP_STRUCT);

#ifdef FEATURE_SIMD
    // storing of TYP_SIMD12 (i.e. Vector3) field
    if (targetType == TYP_SIMD12)
    {
        genStoreLclTypeSimd12(tree);
        return;
    }
#endif // FEATURE_SIMD

    GenTree*   op1       = tree->gtGetOp1();
    regNumber  targetReg = tree->GetRegNum();
    unsigned   lclNum    = tree->GetLclNum();
    LclVarDsc* varDsc    = compiler->lvaGetDesc(lclNum);

    assert(varTypeUsesSameRegType(targetType, op1));
    assert(genTypeSize(genActualType(targetType)) == genTypeSize(genActualType(op1->TypeGet())));

    genConsumeRegs(op1);

    if (op1->OperIs(GT_BITCAST) && op1->isContained())
    {
        GenTree*  bitCastSrc = op1->gtGetOp1();
        var_types srcType    = bitCastSrc->TypeGet();
        noway_assert(!bitCastSrc->isContained());

        if (targetReg == REG_NA)
        {
            GetEmitter()->emitIns_S_R(ins_Store(srcType, compiler->isSIMDTypeLocalAligned(lclNum)),
                                      emitTypeSize(targetType), bitCastSrc->GetRegNum(), lclNum, tree->GetLclOffs());
        }
        else
        {
            genBitCast(targetType, targetReg, srcType, bitCastSrc->GetRegNum());
        }
    }
    else
    {
        GetEmitter()->emitInsBinary(ins_Store(targetType), emitTypeSize(tree), tree, op1);
    }
    genUpdateLifeStore(tree, targetReg, varDsc);
}

//------------------------------------------------------------------------
// genCodeForStoreLclVar: Produce code for a GT_STORE_LCL_VAR node.
//
// Arguments:
//    lclNode - the GT_STORE_LCL_VAR node
//
void CodeGen::genCodeForStoreLclVar(GenTreeLclVar* lclNode)
{
    assert(lclNode->OperIs(GT_STORE_LCL_VAR));

    regNumber targetReg = lclNode->GetRegNum();
    emitter*  emit      = GetEmitter();

    GenTree* op1 = lclNode->gtGetOp1();

    // Stores from a multi-reg source are handled separately.
    if (op1->gtSkipReloadOrCopy()->IsMultiRegNode())
    {
        genMultiRegStoreToLocal(lclNode);
    }
    else
    {
        unsigned   lclNum = lclNode->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);

        var_types targetType = varDsc->GetRegisterType(lclNode);

#ifdef DEBUG
        var_types op1Type = op1->TypeGet();
        if (op1Type == TYP_STRUCT)
        {
            assert(op1->IsLocal());
            GenTreeLclVar* op1LclVar = op1->AsLclVar();
            unsigned       op1lclNum = op1LclVar->GetLclNum();
            LclVarDsc*     op1VarDsc = compiler->lvaGetDesc(op1lclNum);
            op1Type                  = op1VarDsc->GetRegisterType(op1LclVar);
        }
        assert(varTypeUsesSameRegType(targetType, op1Type));
        assert(varTypeUsesIntReg(targetType) || (emitTypeSize(targetType) == emitTypeSize(op1Type)));
#endif

#if !defined(TARGET_64BIT)
        if (targetType == TYP_LONG)
        {
            genStoreLongLclVar(lclNode);
            return;
        }
#endif // !defined(TARGET_64BIT)

#ifdef FEATURE_SIMD
        // storing of TYP_SIMD12 (i.e. Vector3) field
        if (targetType == TYP_SIMD12)
        {
            genStoreLclTypeSimd12(lclNode);
            return;
        }
#endif // FEATURE_SIMD

        genConsumeRegs(op1);

        if (op1->OperIs(GT_BITCAST) && op1->isContained())
        {
            GenTree*  bitCastSrc = op1->gtGetOp1();
            var_types srcType    = bitCastSrc->TypeGet();
            noway_assert(!bitCastSrc->isContained());
            if (targetReg == REG_NA)
            {
                emit->emitIns_S_R(ins_Store(srcType, compiler->isSIMDTypeLocalAligned(lclNum)),
                                  emitTypeSize(targetType), bitCastSrc->GetRegNum(), lclNum, 0);
            }
            else
            {
                genBitCast(targetType, targetReg, srcType, bitCastSrc->GetRegNum());
            }
        }
        else if (targetReg == REG_NA)
        {
            // stack store
            emit->emitInsStoreLcl(ins_Store(targetType, compiler->isSIMDTypeLocalAligned(lclNum)),
                                  emitTypeSize(targetType), lclNode);
        }
        else
        {
            // Look for the case where we have a constant zero which we've marked for reuse,
            // but which isn't actually in the register we want.  In that case, it's better to create
            // zero in the target register, because an xor is smaller than a copy. Note that we could
            // potentially handle this in the register allocator, but we can't always catch it there
            // because the target may not have a register allocated for it yet.
            if (op1->isUsedFromReg() && (op1->GetRegNum() != targetReg) &&
                (op1->IsIntegralConst(0) || op1->IsFloatPositiveZero()))
            {
                op1->SetRegNum(REG_NA);
                op1->ResetReuseRegVal();
                op1->SetContained();
            }

            if (!op1->isUsedFromReg())
            {
                // Currently, we assume that the non-reg source of a GT_STORE_LCL_VAR writing to a register
                // must be a constant. However, in the future we might want to support an operand used from
                // memory.  This is a bit tricky because we have to decide it can be used from memory before
                // register allocation,
                // and this would be a case where, once that's done, we need to mark that node as always
                // requiring a register - which we always assume now anyway, but once we "optimize" that
                // we'll have to take cases like this into account.
                assert((op1->GetRegNum() == REG_NA) && op1->OperIsConst());
                genSetRegToConst(targetReg, targetType, op1);
            }
            else
            {
                assert(targetReg == lclNode->GetRegNum());
                assert(op1->GetRegNum() != REG_NA);
                inst_Mov_Extend(targetType, /* srcInReg */ true, targetReg, op1->GetRegNum(), /* canSkip */ true,
                                emitTypeSize(targetType));
            }
        }
        genUpdateLifeStore(lclNode, targetReg, varDsc);
    }
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

    const regNumber baseReg  = genConsumeReg(base);
    regNumber       indexReg = genConsumeReg(index);
    const regNumber dstReg   = node->GetRegNum();

    // NOTE: `genConsumeReg` marks the consumed register as not a GC pointer, as it assumes that the input registers
    // die at the first instruction generated by the node. This is not the case for `INDEX_ADDR`, however, as the
    // base register is multiply-used. As such, we need to mark the base register as containing a GC pointer until
    // we are finished generating the code for this node.

    gcInfo.gcMarkRegPtrVal(baseReg, base->TypeGet());
    assert(varTypeIsIntegral(index->TypeGet()));

    regNumber tmpReg = REG_NA;
#ifdef TARGET_64BIT
    tmpReg = node->GetSingleTempReg();
#endif

    // Generate the bounds check if necessary.
    if (node->IsBoundsChecked())
    {
#ifdef TARGET_64BIT
        // The CLI Spec allows an array to be indexed by either an int32 or a native int.  In the case that the index
        // is a native int on a 64-bit platform, we will need to widen the array length and then compare.
        if (index->TypeGet() == TYP_I_IMPL)
        {
            GetEmitter()->emitIns_R_AR(INS_mov, EA_4BYTE, tmpReg, baseReg, static_cast<int>(node->gtLenOffset));
            GetEmitter()->emitIns_R_R(INS_cmp, EA_8BYTE, indexReg, tmpReg);
        }
        else
#endif // TARGET_64BIT
        {
            GetEmitter()->emitIns_R_AR(INS_cmp, EA_4BYTE, indexReg, baseReg, static_cast<int>(node->gtLenOffset));
        }

        genJumpToThrowHlpBlk(EJ_jae, SCK_RNGCHK_FAIL, node->gtIndRngFailBB);
    }

#ifdef TARGET_64BIT
    if (index->TypeGet() != TYP_I_IMPL)
    {
        // LEA needs 64-bit operands so we need to widen the index if it's TYP_INT.
        GetEmitter()->emitIns_Mov(INS_mov, EA_4BYTE, tmpReg, indexReg, /* canSkip */ false);
        indexReg = tmpReg;
    }
#endif // TARGET_64BIT

    // Compute the address of the array element.
    unsigned scale = node->gtElemSize;

    switch (scale)
    {
        case 1:
        case 2:
        case 4:
        case 8:
            tmpReg = indexReg;
            break;

        default:
#ifdef TARGET_64BIT
            // IMUL treats its immediate operand as signed so scale can't be larger than INT32_MAX.
            // The VM doesn't allow such large array elements but let's be sure.
            noway_assert(scale <= INT32_MAX);
#else  // !TARGET_64BIT
            tmpReg = node->GetSingleTempReg();
#endif // !TARGET_64BIT

            GetEmitter()->emitIns_R_I(emitter::inst3opImulForReg(tmpReg), EA_PTRSIZE, indexReg,
                                      static_cast<ssize_t>(scale));
            scale = 1;
            break;
    }

    GetEmitter()->emitIns_R_ARX(INS_lea, emitTypeSize(node->TypeGet()), dstReg, baseReg, tmpReg, scale,
                                static_cast<int>(node->gtElemOffset));

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
        genLoadIndTypeSimd12(tree);
        return;
    }
#endif // FEATURE_SIMD

    var_types targetType = tree->TypeGet();
    emitter*  emit       = GetEmitter();

    GenTree* addr = tree->Addr();
    if (addr->IsIconHandle(GTF_ICON_TLS_HDL))
    {
        noway_assert(EA_ATTR(genTypeSize(targetType)) == EA_PTRSIZE);
#if TARGET_64BIT
        emit->emitIns_R_C(ins_Load(TYP_I_IMPL), EA_PTRSIZE, tree->GetRegNum(), FLD_GLOBAL_GS,
                          (int)addr->AsIntCon()->gtIconVal);
#else
        emit->emitIns_R_C(ins_Load(TYP_I_IMPL), EA_PTRSIZE, tree->GetRegNum(), FLD_GLOBAL_FS,
                          (int)addr->AsIntCon()->gtIconVal);
#endif
    }
    else
    {
        genConsumeAddress(addr);
        instruction loadIns = tree->DontExtend() ? INS_mov : ins_Load(targetType);
        emit->emitInsLoadInd(loadIns, emitTypeSize(tree), tree->GetRegNum(), tree);
    }

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
    assert(tree->OperIs(GT_STOREIND));

#ifdef FEATURE_SIMD
    // Storing Vector3 of size 12 bytes through indirection
    if (tree->TypeGet() == TYP_SIMD12)
    {
        genStoreIndTypeSimd12(tree);
        return;
    }
#endif // FEATURE_SIMD

    GenTree*  data       = tree->Data();
    GenTree*  addr       = tree->Addr();
    var_types targetType = tree->TypeGet();

    assert(!varTypeIsFloating(targetType) || (genTypeSize(targetType) == genTypeSize(data->TypeGet())));

    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(tree);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        // data and addr must be in registers.
        // Consume both registers so that any copies of interfering registers are taken care of.
        genConsumeOperands(tree);

        if (genEmitOptimizedGCWriteBarrier(writeBarrierForm, addr, data))
        {
            return;
        }

        // At this point, we should not have any interference.
        // That is, 'data' must not be in REG_WRITE_BARRIER_DST, as that is where 'addr' must go.
        noway_assert(data->GetRegNum() != REG_WRITE_BARRIER_DST);

        // addr goes in REG_WRITE_BARRIER_DST
        genCopyRegIfNeeded(addr, REG_WRITE_BARRIER_DST);

        // data goes in REG_WRITE_BARRIER_SRC
        genCopyRegIfNeeded(data, REG_WRITE_BARRIER_SRC);

        genGCWriteBarrier(tree, writeBarrierForm);
    }
    else
    {
        bool     dataIsUnary   = false;
        bool     isRMWMemoryOp = tree->IsRMWMemoryOp();
        GenTree* rmwSrc        = nullptr;

        // We must consume the operands in the proper execution order, so that liveness is
        // updated appropriately.
        genConsumeAddress(addr);

        // If tree represents a RMW memory op then its data is a non-leaf node marked as contained
        // and non-indir operand of data is the source of RMW memory op.
        if (isRMWMemoryOp)
        {
            assert(data->isContained() && !data->OperIsLeaf());

            GenTree* rmwDst = nullptr;

            dataIsUnary = (GenTree::OperIsUnary(data->OperGet()) != 0);
            if (!dataIsUnary)
            {
                if (tree->IsRMWDstOp1())
                {
                    rmwDst = data->gtGetOp1();
                    rmwSrc = data->gtGetOp2();
                }
                else
                {
                    assert(tree->IsRMWDstOp2());
                    rmwDst = data->gtGetOp2();
                    rmwSrc = data->gtGetOp1();
                }

                genConsumeRegs(rmwSrc);
            }
            else
            {
                // *(p) = oper *(p): Here addr = p, rmwsrc=rmwDst = *(p) i.e. GT_IND(p)
                // For unary RMW ops, src and dst of RMW memory op is the same.  Lower
                // clears operand counts on rmwSrc and we don't need to perform a
                // genConsumeReg() on it.
                assert(tree->IsRMWDstOp1());
                rmwSrc = data->gtGetOp1();
                rmwDst = data->gtGetOp1();
                assert(rmwSrc->isUsedFromMemory());
            }

            assert(rmwSrc != nullptr);
            assert(rmwDst != nullptr);
            assert(Lowering::IndirsAreEquivalent(rmwDst, tree));
        }
        else
        {
            genConsumeRegs(data);
        }

        if (isRMWMemoryOp)
        {
            if (dataIsUnary)
            {
                // generate code for unary RMW memory ops like neg/not
                GetEmitter()->emitInsRMW(genGetInsForOper(data->OperGet(), data->TypeGet()), emitTypeSize(tree), tree);
            }
            else
            {
                if (data->OperIsShiftOrRotate())
                {
                    // Generate code for shift RMW memory ops.
                    // The data address needs to be op1 (it must be [addr] = [addr] <shift> <amount>, not [addr] =
                    // <amount> <shift> [addr]).
                    assert(tree->IsRMWDstOp1());
                    assert(rmwSrc == data->gtGetOp2());
                    genCodeForShiftRMW(tree);
                }
                else if (data->OperIs(GT_ADD) && rmwSrc->isContainedIntOrIImmed() &&
                         (rmwSrc->IsIntegralConst(1) || rmwSrc->IsIntegralConst(-1)))
                {
                    // Generate "inc/dec [mem]" instead of "add/sub [mem], 1".
                    //
                    // Notes:
                    //  1) Global morph transforms GT_SUB(x, +/-1) into GT_ADD(x, -/+1).
                    //  2) TODO-AMD64: Debugger routine NativeWalker::Decode() runs into
                    //     an assert while decoding ModR/M byte of "inc dword ptr [rax]".
                    //     It is not clear whether Decode() can handle all possible
                    //     addr modes with inc/dec.  For this reason, inc/dec [mem]
                    //     is not generated while generating debuggable code.  Update
                    //     the above if condition once Decode() routine is fixed.
                    instruction ins = rmwSrc->IsIntegralConst(1) ? INS_inc : INS_dec;
                    GetEmitter()->emitInsRMW(ins, emitTypeSize(tree), tree);
                }
                else
                {
                    // generate code for remaining binary RMW memory ops like add/sub/and/or/xor
                    GetEmitter()->emitInsRMW(genGetInsForOper(data->OperGet(), data->TypeGet()), emitTypeSize(tree),
                                             tree, rmwSrc);
                }
            }
        }
        else
        {
            instruction ins  = INS_invalid;
            emitAttr    attr = emitTypeSize(tree);

            if (data->isContained())
            {
                if (data->OperIs(GT_BSWAP, GT_BSWAP16))
                {
                    ins = INS_movbe;
                }
#if defined(FEATURE_HW_INTRINSICS)
                else if (data->OperIsHWIntrinsic())
                {
                    GenTreeHWIntrinsic* hwintrinsic = data->AsHWIntrinsic();
                    NamedIntrinsic      intrinsicId = hwintrinsic->GetHWIntrinsicId();
                    var_types           baseType    = hwintrinsic->GetSimdBaseType();

                    switch (intrinsicId)
                    {
                        case NI_Vector128_ToScalar:
                        case NI_Vector256_ToScalar:
                        case NI_Vector512_ToScalar:
                        case NI_SSE2_ConvertToInt32:
                        case NI_SSE2_ConvertToUInt32:
                        case NI_SSE2_X64_ConvertToInt64:
                        case NI_SSE2_X64_ConvertToUInt64:
                        case NI_AVX2_ConvertToInt32:
                        case NI_AVX2_ConvertToUInt32:
                        {
                            // These intrinsics are "ins reg/mem, xmm"
                            ins  = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
                            attr = emitActualTypeSize(baseType);
                            break;
                        }

                        case NI_Vector128_GetElement:
                        {
                            assert(baseType == TYP_FLOAT);
                            FALLTHROUGH;
                        }

                        case NI_SSE2_Extract:
                        case NI_SSE41_Extract:
                        case NI_SSE41_X64_Extract:
                        case NI_AVX_ExtractVector128:
                        case NI_AVX2_ExtractVector128:
                        case NI_AVX512F_ExtractVector128:
                        case NI_AVX512F_ExtractVector256:
                        case NI_AVX512DQ_ExtractVector128:
                        case NI_AVX512DQ_ExtractVector256:
                        {
                            // These intrinsics are "ins reg/mem, xmm, imm8"
                            ins  = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
                            attr = emitActualTypeSize(Compiler::getSIMDTypeForSize(hwintrinsic->GetSimdSize()));

                            if (intrinsicId == NI_SSE2_Extract)
                            {
                                // The encoding that supports containment is SSE4.1 only
                                ins = INS_pextrw_sse41;
                            }

                            // The hardware intrinsics take unsigned bytes between [0, 255].
                            // However, the emitter expects "fits in byte" to always be signed
                            // and therefore we need [128, 255] to be sign extended up to fill
                            // the entire constant value.

                            GenTreeIntCon* op2  = hwintrinsic->Op(2)->AsIntCon();
                            ssize_t        ival = op2->IconValue();

                            assert((ival >= 0) && (ival <= 255));
                            op2->gtIconVal = static_cast<int8_t>(ival);
                            break;
                        }

                        case NI_AVX512F_ConvertToVector256Int32:
                        case NI_AVX512F_ConvertToVector256UInt32:
                        case NI_AVX512F_VL_ConvertToVector128UInt32:
                        case NI_AVX512F_VL_ConvertToVector128UInt32WithSaturation:
                        {
                            assert(!varTypeIsFloating(baseType));
                            FALLTHROUGH;
                        }

                        case NI_AVX512F_ConvertToVector128Byte:
                        case NI_AVX512F_ConvertToVector128ByteWithSaturation:
                        case NI_AVX512F_ConvertToVector128Int16:
                        case NI_AVX512F_ConvertToVector128Int16WithSaturation:
                        case NI_AVX512F_ConvertToVector128SByte:
                        case NI_AVX512F_ConvertToVector128SByteWithSaturation:
                        case NI_AVX512F_ConvertToVector128UInt16:
                        case NI_AVX512F_ConvertToVector128UInt16WithSaturation:
                        case NI_AVX512F_ConvertToVector256Int16:
                        case NI_AVX512F_ConvertToVector256Int16WithSaturation:
                        case NI_AVX512F_ConvertToVector256Int32WithSaturation:
                        case NI_AVX512F_ConvertToVector256UInt16:
                        case NI_AVX512F_ConvertToVector256UInt16WithSaturation:
                        case NI_AVX512F_ConvertToVector256UInt32WithSaturation:
                        case NI_AVX512F_VL_ConvertToVector128Byte:
                        case NI_AVX512F_VL_ConvertToVector128ByteWithSaturation:
                        case NI_AVX512F_VL_ConvertToVector128Int16:
                        case NI_AVX512F_VL_ConvertToVector128Int16WithSaturation:
                        case NI_AVX512F_VL_ConvertToVector128Int32:
                        case NI_AVX512F_VL_ConvertToVector128Int32WithSaturation:
                        case NI_AVX512F_VL_ConvertToVector128SByte:
                        case NI_AVX512F_VL_ConvertToVector128SByteWithSaturation:
                        case NI_AVX512F_VL_ConvertToVector128UInt16:
                        case NI_AVX512F_VL_ConvertToVector128UInt16WithSaturation:
                        case NI_AVX512BW_ConvertToVector256Byte:
                        case NI_AVX512BW_ConvertToVector256ByteWithSaturation:
                        case NI_AVX512BW_ConvertToVector256SByte:
                        case NI_AVX512BW_ConvertToVector256SByteWithSaturation:
                        case NI_AVX512BW_VL_ConvertToVector128Byte:
                        case NI_AVX512BW_VL_ConvertToVector128ByteWithSaturation:
                        case NI_AVX512BW_VL_ConvertToVector128SByte:
                        case NI_AVX512BW_VL_ConvertToVector128SByteWithSaturation:
                        {
                            // These intrinsics are "ins reg/mem, xmm"
                            ins  = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
                            attr = emitActualTypeSize(Compiler::getSIMDTypeForSize(hwintrinsic->GetSimdSize()));
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }
                }
#endif // FEATURE_HW_INTRINSICS
            }

            if (ins == INS_invalid)
            {
                ins = ins_Store(data->TypeGet());
            }

            GetEmitter()->emitInsStoreInd(ins, attr, tree);
        }
    }
}

//------------------------------------------------------------------------
// genCodeForSwap: Produce code for a GT_SWAP node.
//
// Arguments:
//    tree - the GT_SWAP node
//
void CodeGen::genCodeForSwap(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_SWAP));

    // Swap is only supported for lclVar operands that are enregistered
    // We do not consume or produce any registers.  Both operands remain enregistered.
    // However, the gc-ness may change.
    assert(genIsRegCandidateLocal(tree->gtOp1) && genIsRegCandidateLocal(tree->gtOp2));

    GenTreeLclVarCommon* lcl1    = tree->gtOp1->AsLclVarCommon();
    LclVarDsc*           varDsc1 = compiler->lvaGetDesc(lcl1);
    var_types            type1   = varDsc1->TypeGet();
    GenTreeLclVarCommon* lcl2    = tree->gtOp2->AsLclVarCommon();
    LclVarDsc*           varDsc2 = compiler->lvaGetDesc(lcl2);
    var_types            type2   = varDsc2->TypeGet();

    // We must have both int or both fp regs
    assert(varTypeUsesSameRegType(type1, type2));

    // FP swap is not yet implemented (and should have NYI'd in LSRA)
    assert(varTypeUsesIntReg(type1));

    regNumber oldOp1Reg     = lcl1->GetRegNum();
    regMaskTP oldOp1RegMask = genRegMask(oldOp1Reg);
    regNumber oldOp2Reg     = lcl2->GetRegNum();
    regMaskTP oldOp2RegMask = genRegMask(oldOp2Reg);

    // We don't call genUpdateVarReg because we don't have a tree node with the new register.
    varDsc1->SetRegNum(oldOp2Reg);
    varDsc2->SetRegNum(oldOp1Reg);

    // Do the xchg
    emitAttr size = EA_PTRSIZE;
    if (varTypeIsGC(type1) != varTypeIsGC(type2))
    {
        // If the type specified to the emitter is a GC type, it will swap the GC-ness of the registers.
        // Otherwise it will leave them alone, which is correct if they have the same GC-ness.
        size = EA_GCREF;
    }
    inst_RV_RV(INS_xchg, oldOp1Reg, oldOp2Reg, TYP_I_IMPL, size);

    // Update the gcInfo.
    // Manually remove these regs for the gc sets (mostly to avoid confusing duplicative dump output)
    gcInfo.gcRegByrefSetCur &= ~(oldOp1RegMask | oldOp2RegMask);
    gcInfo.gcRegGCrefSetCur &= ~(oldOp1RegMask | oldOp2RegMask);

    // gcMarkRegPtrVal will do the appropriate thing for non-gc types.
    // It will also dump the updates.
    gcInfo.gcMarkRegPtrVal(oldOp2Reg, type1);
    gcInfo.gcMarkRegPtrVal(oldOp1Reg, type2);
}

//------------------------------------------------------------------------
// genEmitOptimizedGCWriteBarrier: Generate write barrier store using the optimized
// helper functions.
//
// Arguments:
//    writeBarrierForm - the write barrier form to use
//    addr - the address at which to do the store
//    data - the data to store
//
// Return Value:
//    true if an optimized write barrier form was used, false if not. If this
//    function returns false, the caller must emit a "standard" write barrier.

bool CodeGen::genEmitOptimizedGCWriteBarrier(GCInfo::WriteBarrierForm writeBarrierForm, GenTree* addr, GenTree* data)
{
    assert(writeBarrierForm != GCInfo::WBF_NoBarrier);

#if defined(TARGET_X86) && NOGC_WRITE_BARRIERS
    if (!genUseOptimizedWriteBarriers(writeBarrierForm))
    {
        return false;
    }

    const static int regToHelper[2][8] = {
        // If the target is known to be in managed memory
        {
            CORINFO_HELP_ASSIGN_REF_EAX, // EAX
            CORINFO_HELP_ASSIGN_REF_ECX, // ECX
            -1,                          // EDX (always the target address)
            CORINFO_HELP_ASSIGN_REF_EBX, // EBX
            -1,                          // ESP
            CORINFO_HELP_ASSIGN_REF_EBP, // EBP
            CORINFO_HELP_ASSIGN_REF_ESI, // ESI
            CORINFO_HELP_ASSIGN_REF_EDI, // EDI
        },

        // Don't know if the target is in managed memory
        {
            CORINFO_HELP_CHECKED_ASSIGN_REF_EAX, // EAX
            CORINFO_HELP_CHECKED_ASSIGN_REF_ECX, // ECX
            -1,                                  // EDX (always the target address)
            CORINFO_HELP_CHECKED_ASSIGN_REF_EBX, // EBX
            -1,                                  // ESP
            CORINFO_HELP_CHECKED_ASSIGN_REF_EBP, // EBP
            CORINFO_HELP_CHECKED_ASSIGN_REF_ESI, // ESI
            CORINFO_HELP_CHECKED_ASSIGN_REF_EDI, // EDI
        },
    };

    noway_assert(regToHelper[0][REG_EAX] == CORINFO_HELP_ASSIGN_REF_EAX);
    noway_assert(regToHelper[0][REG_ECX] == CORINFO_HELP_ASSIGN_REF_ECX);
    noway_assert(regToHelper[0][REG_EBX] == CORINFO_HELP_ASSIGN_REF_EBX);
    noway_assert(regToHelper[0][REG_ESP] == -1);
    noway_assert(regToHelper[0][REG_EBP] == CORINFO_HELP_ASSIGN_REF_EBP);
    noway_assert(regToHelper[0][REG_ESI] == CORINFO_HELP_ASSIGN_REF_ESI);
    noway_assert(regToHelper[0][REG_EDI] == CORINFO_HELP_ASSIGN_REF_EDI);

    noway_assert(regToHelper[1][REG_EAX] == CORINFO_HELP_CHECKED_ASSIGN_REF_EAX);
    noway_assert(regToHelper[1][REG_ECX] == CORINFO_HELP_CHECKED_ASSIGN_REF_ECX);
    noway_assert(regToHelper[1][REG_EBX] == CORINFO_HELP_CHECKED_ASSIGN_REF_EBX);
    noway_assert(regToHelper[1][REG_ESP] == -1);
    noway_assert(regToHelper[1][REG_EBP] == CORINFO_HELP_CHECKED_ASSIGN_REF_EBP);
    noway_assert(regToHelper[1][REG_ESI] == CORINFO_HELP_CHECKED_ASSIGN_REF_ESI);
    noway_assert(regToHelper[1][REG_EDI] == CORINFO_HELP_CHECKED_ASSIGN_REF_EDI);

    regNumber reg = data->GetRegNum();
    noway_assert((reg != REG_ESP) && (reg != REG_OPTIMIZED_WRITE_BARRIER_DST));

    // Generate the following code:
    //            lea     edx, addr
    //            call    write_barrier_helper_reg

    // addr goes in REG_OPTIMIZED_WRITE_BARRIER_DST
    genCopyRegIfNeeded(addr, REG_OPTIMIZED_WRITE_BARRIER_DST);

    unsigned tgtAnywhere = 0;
    if (writeBarrierForm != GCInfo::WBF_BarrierUnchecked)
    {
        tgtAnywhere = 1;
    }

    // Here we might want to call a modified version of genGCWriteBarrier() to get the benefit
    // of the FEATURE_COUNT_GC_WRITE_BARRIERS code. For now, just emit the helper call directly.
    genEmitHelperCall(regToHelper[tgtAnywhere][reg],
                      0,           // argSize
                      EA_PTRSIZE); // retSize

    return true;
#else  // !defined(TARGET_X86) || !NOGC_WRITE_BARRIERS
    return false;
#endif // !defined(TARGET_X86) || !NOGC_WRITE_BARRIERS
}

// Produce code for a GT_CALL node
void CodeGen::genCall(GenTreeCall* call)
{
    genAlignStackBeforeCall(call);

    // all virtuals should have been expanded into a control expression
    assert(!call->IsVirtual() || call->gtControlExpr || call->gtCallAddr);

    // Insert a GS check if necessary
    if (call->IsTailCallViaJitHelper())
    {
        if (compiler->getNeedsGSSecurityCookie())
        {
#if FEATURE_FIXED_OUT_ARGS
            // If either of the conditions below is true, we will need a temporary register in order to perform the GS
            // cookie check. When FEATURE_FIXED_OUT_ARGS is disabled, we save and restore the temporary register using
            // push/pop. When FEATURE_FIXED_OUT_ARGS is enabled, however, we need an alternative solution. For now,
            // though, the tail prefix is ignored on all platforms that use fixed out args, so we should never hit this
            // case.
            assert(compiler->gsGlobalSecurityCookieAddr == nullptr);
            assert((int)compiler->gsGlobalSecurityCookieVal == (ssize_t)compiler->gsGlobalSecurityCookieVal);
#endif
            genEmitGSCookieCheck(true);
        }
    }

    // Consume all the arg regs
    for (CallArg& arg : call->gtArgs.LateArgs())
    {
        CallArgABIInformation& abiInfo = arg.AbiInfo;
        GenTree*               argNode = arg.GetLateNode();

        if (abiInfo.GetRegNum() == REG_STK)
        {
            continue;
        }

#ifdef UNIX_AMD64_ABI
        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            unsigned regIndex = 0;
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
                GenTree* putArgRegNode = use.GetNode();
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);
                regNumber argReg = abiInfo.GetRegNum(regIndex++);

                genConsumeReg(putArgRegNode);

                // Validate the putArgRegNode has the right type.
                assert(varTypeUsesFloatReg(putArgRegNode->TypeGet()) == genIsValidFloatReg(argReg));
                inst_Mov_Extend(putArgRegNode->TypeGet(), /* srcInReg */ false, argReg, putArgRegNode->GetRegNum(),
                                /* canSkip */ true, emitActualTypeSize(TYP_I_IMPL));
            }
        }
        else
#endif // UNIX_AMD64_ABI
        {
            regNumber argReg = abiInfo.GetRegNum();
            genConsumeReg(argNode);
            inst_Mov_Extend(argNode->TypeGet(), /* srcInReg */ false, argReg, argNode->GetRegNum(), /* canSkip */ true,
                            emitActualTypeSize(TYP_I_IMPL));
        }

        // In the case of a varargs call,
        // the ABI dictates that if we have floating point args,
        // we must pass the enregistered arguments in both the
        // integer and floating point registers so, let's do that.
        if (compFeatureVarArg() && call->IsVarargs() && varTypeIsFloating(argNode))
        {
            regNumber srcReg    = argNode->GetRegNum();
            regNumber targetReg = compiler->getCallArgIntRegister(argNode->GetRegNum());
            inst_Mov(TYP_LONG, targetReg, srcReg, /* canSkip */ false, emitActualTypeSize(TYP_I_IMPL));
        }
    }

#if defined(TARGET_X86) || defined(UNIX_AMD64_ABI)
    // The call will pop its arguments.
    // for each putarg_stk:
    target_ssize_t stackArgBytes = 0;
    for (CallArg& arg : call->gtArgs.EarlyArgs())
    {
        GenTree* argNode = arg.GetEarlyNode();
        if (argNode->OperIs(GT_PUTARG_STK) && (arg.GetLateNode() == nullptr))
        {
            GenTree* source  = argNode->AsPutArgStk()->gtGetOp1();
            unsigned argSize = argNode->AsPutArgStk()->GetStackByteSize();
            stackArgBytes += argSize;

#ifdef DEBUG
            assert(argSize == arg.AbiInfo.ByteSize);
#ifdef FEATURE_PUT_STRUCT_ARG_STK
            if (source->TypeIs(TYP_STRUCT) && !source->OperIs(GT_FIELD_LIST))
            {
                unsigned loadSize = source->GetLayout(compiler)->GetSize();
                assert(argSize == roundUp(loadSize, TARGET_POINTER_SIZE));
            }
#endif // FEATURE_PUT_STRUCT_ARG_STK
#endif // DEBUG
        }
    }
#endif // defined(TARGET_X86) || defined(UNIX_AMD64_ABI)

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);
        GetEmitter()->emitIns_AR_R(INS_cmp, EA_4BYTE, regThis, regThis, 0);
    }

    // If fast tail call, then we are done here, we just have to load the call
    // target into the right registers. We ensure in RA that the registers used
    // for the target (e.g. contained indir) are loaded into volatile registers
    // that won't be restored by epilog sequence.
    if (call->IsFastTailCall())
    {
        GenTree* target = getCallTarget(call, nullptr);
        if (target != nullptr)
        {
            if (target->isContainedIndir())
            {
                genConsumeAddress(target->AsIndir()->Addr());
            }
            else
            {
                assert(!target->isContained());
                genConsumeReg(target);
            }
        }

        return;
    }

    // For a pinvoke to unmanged code we emit a label to clear
    // the GC pointer state before the callsite.
    // We can't utilize the typical lazy killing of GC pointers
    // at (or inside) the callsite.
    if (compiler->killGCRefs(call))
    {
        genDefineTempLabel(genCreateTempLabel());
    }

#if defined(DEBUG) && defined(TARGET_X86)
    // Store the stack pointer so we can check it after the call.
    if (compiler->opts.compStackCheckOnCall && call->gtCallType == CT_USER_FUNC)
    {
        assert(compiler->lvaCallSpCheck != BAD_VAR_NUM);
        assert(compiler->lvaGetDesc(compiler->lvaCallSpCheck)->lvDoNotEnregister);
        assert(compiler->lvaGetDesc(compiler->lvaCallSpCheck)->lvOnFrame);
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaCallSpCheck, 0);
    }
#endif // defined(DEBUG) && defined(TARGET_X86)

    if (GetEmitter()->Contains256bitOrMoreAVX() && call->NeedsVzeroupper(compiler))
    {
        // The Intel optimization manual guidance in `3.11.5.3 Fixing Instruction Slowdowns` states:
        //   Insert a VZEROUPPER to tell the hardware that the state of the higher registers is clean
        //   between the VEX and the legacy SSE instructions. Often the best way to do this is to insert a
        //   VZEROUPPER before returning from any function that uses VEX (that does not produce a VEX
        //   register) and before any call to an unknown function.

        // This method contains a call that needs vzeroupper but also uses 256-bit or higher
        // AVX itself. This means we couldn't optimize to only emitting a single vzeroupper in
        // the method prologue and instead need to insert one before each call that needs it.

        instGen(INS_vzeroupper);
    }

#ifdef SWIFT_SUPPORT
    // Clear the Swift error register before calling a Swift method,
    // so we can check if it set the error register after returning.
    // (Flag is only set if we know we need to check the error register)
    if ((call->gtCallMoreFlags & GTF_CALL_M_SWIFT_ERROR_HANDLING) != 0)
    {
        assert(call->unmgdCallConv == CorInfoCallConvExtension::Swift);
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, REG_SWIFT_ERROR);
    }
#endif // SWIFT_SUPPORT

    genCallInstruction(call X86_ARG(stackArgBytes));

    genDefinePendingCallLabel(call);

#ifdef DEBUG
    // We should not have GC pointers in killed registers live around the call.
    // GC info for arg registers were cleared when consuming arg nodes above
    // and LSRA should ensure it for other trashed registers.
    regMaskTP killMask = RBM_CALLEE_TRASH;
    if (call->IsHelperCall())
    {
        CorInfoHelpFunc helpFunc = compiler->eeGetHelperNum(call->gtCallMethHnd);
        killMask                 = compiler->compHelperCallKillSet(helpFunc);
    }

    assert((gcInfo.gcRegGCrefSetCur & killMask) == 0);
    assert((gcInfo.gcRegByrefSetCur & killMask) == 0);
#endif

    var_types returnType = call->TypeGet();
    if (returnType != TYP_VOID)
    {
#ifdef TARGET_X86
        if (varTypeIsFloating(returnType))
        {
            // Spill the value from the fp stack.
            // Then, load it into the target register.
            call->gtFlags |= GTF_SPILL;
            regSet.rsSpillFPStack(call);
            call->gtFlags |= GTF_SPILLED;
            call->gtFlags &= ~GTF_SPILL;
        }
        else
#endif // TARGET_X86
        {
            regNumber returnReg;

            if (call->HasMultiRegRetVal())
            {
                const ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
                assert(retTypeDesc != nullptr);
                const unsigned regCount = retTypeDesc->GetReturnRegCount();

                // If regs allocated to call node are different from ABI return
                // regs in which the call has returned its result, move the result
                // to regs allocated to call node.
                for (unsigned i = 0; i < regCount; ++i)
                {
                    var_types regType      = retTypeDesc->GetReturnRegType(i);
                    returnReg              = retTypeDesc->GetABIReturnReg(i);
                    regNumber allocatedReg = call->GetRegNumByIdx(i);
                    inst_Mov(regType, allocatedReg, returnReg, /* canSkip */ true);
                }

#if defined(FEATURE_SIMD)
                // A Vector3 return value is stored in xmm0 and xmm1.
                // RyuJIT assumes that the upper unused bits of xmm1 are cleared but
                // the native compiler doesn't guarantee it.
                if (call->IsUnmanaged() && (returnType == TYP_SIMD12))
                {
                    returnReg = retTypeDesc->GetABIReturnReg(1);
                    genSimd12UpperClear(returnReg);
                }
#endif // FEATURE_SIMD
            }
            else
            {
#ifdef TARGET_X86
                if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
                {
                    // The x86 CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
                    // TCB in REG_PINVOKE_TCB. AMD64/ARM64 use the standard calling convention. fgMorphCall() sets the
                    // correct argument registers.
                    returnReg = REG_PINVOKE_TCB;
                }
                else
#endif // TARGET_X86
                    if (varTypeIsFloating(returnType))
                {
                    returnReg = REG_FLOATRET;
                }
                else
                {
                    returnReg = REG_INTRET;
                }

                inst_Mov(returnType, call->GetRegNum(), returnReg, /* canSkip */ true);
            }

            genProduceReg(call);
        }
    }

    // If there is nothing next, that means the result is thrown away, so this value is not live.
    // However, for minopts or debuggable code, we keep it live to support managed return value debugging.
    if ((call->gtNext == nullptr) && compiler->opts.OptimizationEnabled())
    {
        gcInfo.gcMarkRegSetNpt(RBM_INTRET);
    }

#if defined(DEBUG) && defined(TARGET_X86)
    // REG_ARG_0 (ECX) is trashed, so can be used as a temporary register.
    genStackPointerCheck(compiler->opts.compStackCheckOnCall && (call->gtCallType == CT_USER_FUNC),
                         compiler->lvaCallSpCheck, call->CallerPop() ? 0 : stackArgBytes, REG_ARG_0);
#endif // defined(DEBUG) && defined(TARGET_X86)

#if !defined(FEATURE_EH_FUNCLETS)
    //-------------------------------------------------------------------------
    // Create a label for tracking of region protected by the monitor in synchronized methods.
    // This needs to be here, rather than above where fPossibleSyncHelperCall is set,
    // so the GC state vars have been updated before creating the label.

    if ((call->gtCallType == CT_HELPER) && (compiler->info.compFlags & CORINFO_FLG_SYNCH))
    {
        CorInfoHelpFunc helperNum = compiler->eeGetHelperNum(call->gtCallMethHnd);
        noway_assert(helperNum != CORINFO_HELP_UNDEF);
        switch (helperNum)
        {
            case CORINFO_HELP_MON_ENTER:
            case CORINFO_HELP_MON_ENTER_STATIC:
                noway_assert(compiler->syncStartEmitCookie == nullptr);
                compiler->syncStartEmitCookie =
                    GetEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);
                noway_assert(compiler->syncStartEmitCookie != nullptr);
                break;
            case CORINFO_HELP_MON_EXIT:
            case CORINFO_HELP_MON_EXIT_STATIC:
                noway_assert(compiler->syncEndEmitCookie == nullptr);
                compiler->syncEndEmitCookie =
                    GetEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);
                noway_assert(compiler->syncEndEmitCookie != nullptr);
                break;
            default:
                break;
        }
    }
#endif // !FEATURE_EH_FUNCLETS

    unsigned stackAdjustBias = 0;

#if defined(TARGET_X86)
    // Is the caller supposed to pop the arguments?
    if (call->CallerPop() && (stackArgBytes != 0))
    {
        stackAdjustBias = stackArgBytes;
    }

    SubtractStackLevel(stackArgBytes);
#endif // TARGET_X86

    genRemoveAlignmentAfterCall(call, stackAdjustBias);
}

//------------------------------------------------------------------------
// genCallInstruction - Generate instructions necessary to transfer control to the call.
//
// Arguments:
//    call - the GT_CALL node
//
// Remaks:
//   For tailcalls this function will generate a jump.
//
void CodeGen::genCallInstruction(GenTreeCall* call X86_ARG(target_ssize_t stackArgBytes))
{
#if defined(TARGET_X86)
    // If the callee pops the arguments, we pass a positive value as the argSize, and the emitter will
    // adjust its stack level accordingly.
    // If the caller needs to explicitly pop its arguments, we must pass a negative value, and then do the
    // pop when we're done.
    target_ssize_t argSizeForEmitter = stackArgBytes;
    if (call->CallerPop())
    {
        argSizeForEmitter = -stackArgBytes;
    }
#endif // defined(TARGET_X86)

    // Determine return value size(s).
    const ReturnTypeDesc* retTypeDesc   = call->GetReturnTypeDesc();
    emitAttr              retSize       = EA_PTRSIZE;
    emitAttr              secondRetSize = EA_UNKNOWN;

    if (call->HasMultiRegRetVal())
    {
        retSize       = emitTypeSize(retTypeDesc->GetReturnRegType(0));
        secondRetSize = emitTypeSize(retTypeDesc->GetReturnRegType(1));
    }
    else
    {
        assert(!varTypeIsStruct(call));

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

    DebugInfo di;

    if (compiler->opts.compDbgInfo && compiler->genCallSite2DebugInfoMap != nullptr && !call->IsTailCall())
    {
        (void)compiler->genCallSite2DebugInfoMap->Lookup(call, &di);
    }

    CORINFO_SIG_INFO* sigInfo = nullptr;
#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (call->gtCallType != CT_HELPER)
    {
        sigInfo = call->callSig;
    }
#endif // DEBUG

    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = getCallTarget(call, &methHnd);
    if (target != nullptr)
    {
#ifdef TARGET_X86
        if (call->IsVirtualStub() && (call->gtCallType == CT_INDIRECT))
        {
            // On x86, we need to generate a very specific pattern for indirect VSD calls:
            //
            //    3-byte nop
            //    call dword ptr [eax]
            //
            // Where EAX is also used as an argument to the stub dispatch helper. Make
            // sure that the call target address is computed into EAX in this case.

            assert(compiler->virtualStubParamInfo->GetReg() == REG_VIRTUAL_STUB_TARGET);

            assert(target->isContainedIndir());
            assert(target->OperGet() == GT_IND);

            GenTree* addr = target->AsIndir()->Addr();
            assert(addr->isUsedFromReg());

            genConsumeReg(addr);
            genCopyRegIfNeeded(addr, REG_VIRTUAL_STUB_TARGET);

            GetEmitter()->emitIns_Nop(3);

            // clang-format off
            GetEmitter()->emitIns_Call(emitter::EC_INDIR_ARD,
                                       methHnd,
                                       INDEBUG_LDISASM_COMMA(sigInfo)
                                       nullptr,
                                       argSizeForEmitter,
                                       retSize
                                       MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                                       gcInfo.gcVarPtrSetCur,
                                       gcInfo.gcRegGCrefSetCur,
                                       gcInfo.gcRegByrefSetCur,
                                       di, REG_VIRTUAL_STUB_TARGET, REG_NA, 1, 0);
            // clang-format on
        }
        else
#endif
            if (target->isContainedIndir())
        {
            // When CFG is enabled we should not be emitting any non-register indirect calls.
            assert(!compiler->opts.IsCFGEnabled() ||
                   call->IsHelperCall(compiler, CORINFO_HELP_VALIDATE_INDIRECT_CALL) ||
                   call->IsHelperCall(compiler, CORINFO_HELP_DISPATCH_INDIRECT_CALL));

            if (target->AsIndir()->HasBase() && target->AsIndir()->Base()->isContainedIntOrIImmed())
            {
                // Note that if gtControlExpr is an indir of an absolute address, we mark it as
                // contained only if it can be encoded as PC-relative offset.
                assert(target->AsIndir()->Base()->AsIntConCommon()->FitsInAddrBase(compiler));

                // clang-format off
                genEmitCall(emitter::EC_FUNC_TOKEN_INDIR,
                            methHnd,
                            INDEBUG_LDISASM_COMMA(sigInfo)
                            (void*) target->AsIndir()->Base()->AsIntConCommon()->IconValue()
                            X86_ARG(argSizeForEmitter),
                            retSize
                            MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                            di,
                            REG_NA,
                            call->IsFastTailCall());
                // clang-format on
            }
            else
            {
                // For fast tailcalls this is happening in epilog, so we should
                // have already consumed target in genCall.
                if (!call->IsFastTailCall())
                {
                    genConsumeAddress(target->AsIndir()->Addr());
                }

                // clang-format off
                genEmitCallIndir(emitter::EC_INDIR_ARD,
                                 methHnd,
                                 INDEBUG_LDISASM_COMMA(sigInfo)
                                 target->AsIndir()
                                 X86_ARG(argSizeForEmitter),
                                 retSize
                                 MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                                 di,
                                 call->IsFastTailCall());
                // clang-format on
            }
        }
        else
        {
            if (!compiler->IsTargetAbi(CORINFO_NATIVEAOT_ABI) || (call->gtFlags & GTF_TLS_GET_ADDR) == 0)
            {
                // We have already generated code for gtControlExpr evaluating it into a register.
                // We just need to emit "call reg" in this case.
                assert(genIsValidIntReg(target->GetRegNum()));

                // For fast tailcalls this is happening in epilog, so we should
                // have already consumed target in genCall.
                if (!call->IsFastTailCall())
                {
                    genConsumeReg(target);
                }

                // clang-format off
                genEmitCall(emitter::EC_INDIR_R,
                            methHnd,
                            INDEBUG_LDISASM_COMMA(sigInfo)
                            nullptr // addr
                            X86_ARG(argSizeForEmitter),
                            retSize
                            MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                            di,
                            target->GetRegNum(),
                            call->IsFastTailCall());
                // clang-format on
            }
            else
            {
                GenTree* tlsGetAddr = (GenTree*)call->gtCallMethHnd;

                // NativeAOT code needs special code sequence prefix of call so the
                // linker will do the fixup and emit accurate TLS access information.
                GetEmitter()->emitIns_Data16();
                GetEmitter()->emitIns_Data16();

                // clang-format off
                genEmitCall(emitter::EC_FUNC_TOKEN,
                            (CORINFO_METHOD_HANDLE)1,
                            INDEBUG_LDISASM_COMMA(sigInfo)
                            (void*)tlsGetAddr->AsIntCon()->gtIconVal // addr
                            X86_ARG(argSizeForEmitter),
                            retSize
                            MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                            di,
                            target->GetRegNum(),
                            call->IsFastTailCall());
                // clang-format on
            }
        }
    }
    else
    {
        // If we have no target and this is a call with indirection cell
        // then emit call through that indir cell. This means we generate e.g.
        // lea r11, [addr of cell]
        // call [r11]
        // which is more efficient than
        // lea r11, [addr of cell]
        // call [addr of cell]
        regNumber indirCellReg = getCallIndirectionCellReg(call);
        if (indirCellReg != REG_NA)
        {
            // clang-format off
            GetEmitter()->emitIns_Call(
                emitter::EC_INDIR_ARD,
                methHnd,
                INDEBUG_LDISASM_COMMA(sigInfo)
                nullptr,
                0,
                retSize
                MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                gcInfo.gcVarPtrSetCur,
                gcInfo.gcRegGCrefSetCur,
                gcInfo.gcRegByrefSetCur,
                di, indirCellReg, REG_NA, 0, 0,
                call->IsFastTailCall());
            // clang-format on
        }
#ifdef FEATURE_READYTORUN
        else if (call->gtEntryPoint.addr != nullptr)
        {
            emitter::EmitCallType type =
                (call->gtEntryPoint.accessType == IAT_VALUE) ? emitter::EC_FUNC_TOKEN : emitter::EC_FUNC_TOKEN_INDIR;
            // clang-format off
            genEmitCall(type,
                        methHnd,
                        INDEBUG_LDISASM_COMMA(sigInfo)
                        (void*)call->gtEntryPoint.addr
                        X86_ARG(argSizeForEmitter),
                        retSize
                        MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                        di,
                        REG_NA,
                        call->IsFastTailCall());
            // clang-format on
        }
#endif
        else
        {
            // Generate a direct call to a non-virtual user defined or helper method
            assert(call->gtCallType == CT_HELPER || call->gtCallType == CT_USER_FUNC);

            void* addr = nullptr;
            if (call->gtCallType == CT_HELPER)
            {
                // Direct call to a helper method.
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

            // Non-virtual direct calls to known addresses

            // clang-format off
            genEmitCall(emitter::EC_FUNC_TOKEN,
                        methHnd,
                        INDEBUG_LDISASM_COMMA(sigInfo)
                        addr
                        X86_ARG(argSizeForEmitter),
                        retSize
                        MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                        di,
                        REG_NA,
                        call->IsFastTailCall());
            // clang-format on
        }
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
    for (varNum = 0; varNum < compiler->info.compArgsCount; varNum++)
    {
        varDsc = compiler->lvaGetDesc(varNum);

        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaGetDesc(fieldVarNum);
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
            {
                continue;
            }
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

        assert(!varDsc->lvIsStructField || (compiler->lvaGetDesc(varDsc->lvParentLcl)->lvFieldCnt == 1));
        var_types storeType = varDsc->GetStackSlotHomeType(); // We own the memory and can use the full move.
        GetEmitter()->emitIns_S_R(ins_Store(storeType), emitTypeSize(storeType), varDsc->GetRegNum(), varNum, 0);

        // Update lvRegNum life and GC info to indicate lvRegNum is dead and varDsc stack slot is going live.
        // Note that we cannot modify varDsc->GetRegNum() here because another basic block may not be expecting it.
        // Therefore manually update life of varDsc->GetRegNum().
        regMaskTP tempMask = varDsc->lvRegMask();
        regSet.RemoveMaskVars(tempMask);
        gcInfo.gcMarkRegSetNpt(tempMask);
        if (compiler->lvaIsGCTracked(varDsc))
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
#endif // DEBUG

            VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
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
    for (varNum = 0; varNum < compiler->info.compArgsCount; varNum++)
    {
        varDsc = compiler->lvaGetDesc(varNum);
        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaGetDesc(fieldVarNum);
        }
        noway_assert(varDsc->lvIsParam);

        // Skip if arg not passed in a register.
        if (!varDsc->lvIsRegArg)
        {
            continue;
        }

#if defined(UNIX_AMD64_ABI)
        if (varTypeIsStruct(varDsc))
        {
            CORINFO_CLASS_HANDLE typeHnd = varDsc->GetLayout()->GetClassHandle();
            assert(typeHnd != nullptr);

            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            compiler->eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
            assert(structDesc.passedInRegisters);

            unsigned __int8 offset0 = 0;
            unsigned __int8 offset1 = 0;
            var_types       type0   = TYP_UNKNOWN;
            var_types       type1   = TYP_UNKNOWN;

            // Get the eightbyte data
            compiler->GetStructTypeOffset(structDesc, &type0, &type1, &offset0, &offset1);

            // Move the values into the right registers.
            //

            // Update varDsc->GetArgReg() and lvOtherArgReg life and GC Info to indicate varDsc stack slot is dead and
            // argReg is going live. Note that we cannot modify varDsc->GetRegNum() and lvOtherArgReg here
            // because another basic block may not be expecting it.
            // Therefore manually update life of argReg.  Note that GT_JMP marks
            // the end of the basic block and after which reg life and gc info will be recomputed for the new block in
            // genCodeForBBList().
            if (type0 != TYP_UNKNOWN)
            {
                GetEmitter()->emitIns_R_S(ins_Load(type0), emitTypeSize(type0), varDsc->GetArgReg(), varNum, offset0);
                regSet.SetMaskVars(regSet.GetMaskVars() | genRegMask(varDsc->GetArgReg()));
                gcInfo.gcMarkRegPtrVal(varDsc->GetArgReg(), type0);
            }

            if (type1 != TYP_UNKNOWN)
            {
                GetEmitter()->emitIns_R_S(ins_Load(type1), emitTypeSize(type1), varDsc->GetOtherArgReg(), varNum,
                                          offset1);
                regSet.SetMaskVars(regSet.GetMaskVars() | genRegMask(varDsc->GetOtherArgReg()));
                gcInfo.gcMarkRegPtrVal(varDsc->GetOtherArgReg(), type1);
            }

            if (varDsc->lvTracked)
            {
                VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
            }
        }
        else
#endif // !defined(UNIX_AMD64_ABI)
        {
            // Register argument
            CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef TARGET_X86
            noway_assert(isRegParamType(genActualType(varDsc->TypeGet())) ||
                         ((varDsc->TypeGet() == TYP_STRUCT) &&
                          compiler->isTrivialPointerSizedStruct(varDsc->GetLayout()->GetClassHandle())));
#else
            noway_assert(isRegParamType(genActualType(varDsc->TypeGet())));
#endif // TARGET_X86

            // Is register argument already in the right register?
            // If not load it from its stack location.
            var_types loadType = varDsc->GetRegisterType();

#ifdef TARGET_X86
            if (varTypeIsStruct(varDsc->TypeGet()))
            {
                // Treat trivial pointer-sized structs as a pointer sized primitive
                // for the purposes of registers.
                loadType = TYP_I_IMPL;
            }
#endif

            regNumber argReg = varDsc->GetArgReg(); // incoming arg register

            if (varDsc->GetRegNum() != argReg)
            {
                assert(genIsValidReg(argReg));
                GetEmitter()->emitIns_R_S(ins_Load(loadType), emitTypeSize(loadType), argReg, varNum, 0);

                // Update argReg life and GC Info to indicate varDsc stack slot is dead and argReg is going live.
                // Note that we cannot modify varDsc->GetRegNum() here because another basic block may not be
                // expecting it. Therefore manually update life of argReg.  Note that GT_JMP marks the end of the
                // basic block and after which reg life and gc info will be recomputed for the new block in
                // genCodeForBBList().
                regSet.AddMaskVars(genRegMask(argReg));
                gcInfo.gcMarkRegPtrVal(argReg, loadType);
                if (compiler->lvaIsGCTracked(varDsc))
                {
#ifdef DEBUG
                    if (VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
                    {
                        JITDUMP("\t\t\t\t\t\t\tVar V%02u becoming dead\n", varNum);
                    }
                    else
                    {
                        JITDUMP("\t\t\t\t\t\t\tVar V%02u continuing dead\n", varNum);
                    }
#endif // DEBUG

                    VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
                }
            }
        }

#if defined(TARGET_AMD64)
        // In case of a jmp call to a vararg method also pass the float/double arg in the corresponding int arg
        // register. This is due to the AMD64 ABI which requires floating point values passed to varargs functions to
        // be passed in both integer and floating point registers. It doesn't apply to x86, which passes floating point
        // values on the stack.
        if (compFeatureVarArg() && compiler->info.compIsVarArgs)
        {
            regNumber intArgReg;
            var_types loadType = varDsc->GetRegisterType();
            regNumber argReg   = varDsc->GetArgReg(); // incoming arg register

            if (varTypeIsFloating(loadType))
            {
                intArgReg = compiler->getCallArgIntRegister(argReg);
                inst_Mov(TYP_LONG, intArgReg, argReg, /* canSkip */ false, emitActualTypeSize(loadType));
            }
            else
            {
                intArgReg = argReg;
            }

            fixedIntArgMask |= genRegMask(intArgReg);

            if (intArgReg == REG_ARG_0)
            {
                assert(firstArgVarNum == BAD_VAR_NUM);
                firstArgVarNum = varNum;
            }
        }
#endif // TARGET_AMD64
    }

#if defined(TARGET_AMD64)
    // Jmp call to a vararg method - if the method has fewer than 4 fixed arguments,
    // load the remaining arg registers (both int and float) from the corresponding
    // shadow stack slots.  This is for the reason that we don't know the number and type
    // of non-fixed params passed by the caller, therefore we have to assume the worst case
    // of caller passing float/double args both in int and float arg regs.
    //
    // This doesn't apply to x86, which doesn't pass floating point values in floating
    // point registers.
    //
    // The caller could have passed gc-ref/byref type var args.  Since these are var args
    // the callee no way of knowing their gc-ness.  Therefore, mark the region that loads
    // remaining arg registers from shadow stack slots as non-gc interruptible.
    if (compFeatureVarArg() && fixedIntArgMask != RBM_NONE)
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
                    GetEmitter()->emitIns_R_S(INS_mov, EA_8BYTE, argReg, firstArgVarNum, argOffset);

                    // also load it in corresponding float arg reg
                    regNumber floatReg = compiler->getCallArgFloatRegister(argReg);
                    inst_Mov(TYP_DOUBLE, floatReg, argReg, /* canSkip */ false, emitActualTypeSize(TYP_I_IMPL));
                }

                argOffset += REGSIZE_BYTES;
            }
            GetEmitter()->emitEnableGC();
        }
    }
#endif // TARGET_AMD64
}

// produce code for a GT_LEA subnode
void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    emitAttr size = emitTypeSize(lea);
    genConsumeOperands(lea);

    if (lea->HasBase() && lea->HasIndex())
    {
        regNumber baseReg  = lea->Base()->GetRegNum();
        regNumber indexReg = lea->Index()->GetRegNum();
        GetEmitter()->emitIns_R_ARX(INS_lea, size, lea->GetRegNum(), baseReg, indexReg, lea->gtScale, lea->Offset());
    }
    else if (lea->HasBase())
    {
        GetEmitter()->emitIns_R_AR(INS_lea, size, lea->GetRegNum(), lea->Base()->GetRegNum(), lea->Offset());
    }
    else if (lea->HasIndex())
    {
        GetEmitter()->emitIns_R_ARX(INS_lea, size, lea->GetRegNum(), REG_NA, lea->Index()->GetRegNum(), lea->gtScale,
                                    lea->Offset());
    }

    genProduceReg(lea);
}

//------------------------------------------------------------------------
// genCompareFloat: Generate code for comparing two floating point values
//
// Arguments:
//    treeNode - the compare tree
//
void CodeGen::genCompareFloat(GenTree* treeNode)
{
    assert(treeNode->OperIsCompare() || treeNode->OperIs(GT_CMP));

    GenTreeOp* tree    = treeNode->AsOp();
    GenTree*   op1     = tree->gtOp1;
    GenTree*   op2     = tree->gtOp2;
    var_types  op1Type = op1->TypeGet();
    var_types  op2Type = op2->TypeGet();

    assert(varTypeIsFloating(op1Type));
    assert(op1Type == op2Type);

    regNumber   targetReg = treeNode->GetRegNum();
    instruction ins;
    emitAttr    cmpAttr;

    GenCondition condition;
    if (!treeNode->OperIs(GT_CMP))
    {
        condition = GenCondition::FromFloatRelop(treeNode);

        if (condition.PreferSwap())
        {
            condition = GenCondition::Swap(condition);
            std::swap(op1, op2);
        }
    }
    else
    {
        assert(targetReg == REG_NA);
    }

    ins     = (op1Type == TYP_FLOAT) ? INS_ucomiss : INS_ucomisd;
    cmpAttr = emitTypeSize(op1Type);

    var_types targetType = treeNode->TypeGet();

    // Clear target reg in advance via "xor reg,reg" to avoid movzx after SETCC
    if ((targetReg != REG_NA) && (op1->GetRegNum() != targetReg) && (op2->GetRegNum() != targetReg) &&
        !varTypeIsByte(targetType))
    {
        regMaskTP targetRegMask = genRegMask(targetReg);
        if (((op1->gtGetContainedRegMask() | op2->gtGetContainedRegMask()) & targetRegMask) == 0)
        {
            instGen_Set_Reg_To_Zero(emitTypeSize(TYP_I_IMPL), targetReg);
            targetType = TYP_UBYTE; // just a tip for inst_SETCC that movzx is not needed
        }
    }
    GetEmitter()->emitInsBinary(ins, cmpAttr, op1, op2);

    // Are we evaluating this into a register?
    if (targetReg != REG_NA)
    {
        if ((condition.GetCode() == GenCondition::FNEU) && op1->isUsedFromReg() && op2->isUsedFromReg() &&
            (op1->GetRegNum() == op2->GetRegNum()))
        {
            // For floating point, `x != x` is a common way of
            // checking for NaN. So, in the case where both
            // operands are the same, we can optimize codegen
            // to only do a single check.

            condition = GenCondition(GenCondition::P);
        }

        inst_SETCC(condition, targetType, targetReg);
        genProduceReg(tree);
    }
}

//------------------------------------------------------------------------
// genCompareInt: Generate code for comparing ints or, on amd64, longs.
//
// Arguments:
//    treeNode - the compare tree
//
// Return Value:
//    None.
void CodeGen::genCompareInt(GenTree* treeNode)
{
    assert(treeNode->OperIsCompare() || treeNode->OperIs(GT_CMP, GT_TEST, GT_BT));

    GenTreeOp* tree          = treeNode->AsOp();
    GenTree*   op1           = tree->gtOp1;
    GenTree*   op2           = tree->gtOp2;
    var_types  op1Type       = op1->TypeGet();
    var_types  op2Type       = op2->TypeGet();
    regNumber  targetReg     = tree->GetRegNum();
    emitter*   emit          = GetEmitter();
    bool       canReuseFlags = false;

    assert(!op1->isContainedIntOrIImmed());
    assert(!varTypeIsFloating(op2Type));

    instruction ins;
    var_types   type = TYP_UNKNOWN;

    if (tree->OperIs(GT_TEST_EQ, GT_TEST_NE, GT_TEST))
    {
        ins = INS_test;

        // Unlike many xarch instructions TEST doesn't have a form with a 16/32/64 bit first operand and
        // an 8 bit immediate second operand. But if the immediate value fits in 8 bits then we can simply
        // emit a 8 bit TEST instruction, unless we're targeting x86 and the first operand is a non-byteable
        // register.
        // Note that lowering does something similar but its main purpose is to allow memory operands to be
        // contained so it doesn't handle other kind of operands. It could do more but on x86 that results
        // in additional register constrains and that may be worse than wasting 3 bytes on an immediate.
        if (
#ifdef TARGET_X86
            (!op1->isUsedFromReg() || isByteReg(op1->GetRegNum())) &&
#endif
            (op2->IsCnsIntOrI() && FitsIn<uint8_t>(op2->AsIntCon()->IconValue())))
        {
            type = TYP_UBYTE;
        }
    }
    else if (tree->OperIs(GT_BITTEST_EQ, GT_BITTEST_NE, GT_BT))
    {
        ins = INS_bt;

        // BT is a bit special in that the index is used modulo 32. We allow
        // mixing the types of op1/op2 because of that -- even if the index is
        // TYP_INT but the op size is TYP_LONG the instruction itself will
        // ignore the upper part of the register anyway.
        type = genActualType(op1->TypeGet());

        // The emitter's general logic handles op1/op2 for bt reversed. As a
        // small hack we reverse it in codegen instead of special casing the
        // emitter throughout.
        std::swap(op1, op2);
    }
    else if (op1->isUsedFromReg() && op2->IsIntegralConst(0))
    {
        if (compiler->opts.OptimizationEnabled())
        {
            emitAttr op1Size = emitActualTypeSize(op1->TypeGet());
            assert((int)op1Size >= 4);

            // Optimize "x<0" and "x>=0" to "x>>31" if "x" is not a jump condition and in a reg.
            // Morph/Lowering are responsible to rotate "0<x" to "x>0" so we won't handle it here.
            if ((targetReg != REG_NA) && tree->OperIs(GT_LT, GT_GE) && !tree->IsUnsigned())
            {
                inst_Mov(op1->TypeGet(), targetReg, op1->GetRegNum(), /* canSkip */ true);
                if (tree->OperIs(GT_GE))
                {
                    // emit "not" for "x>=0" case
                    inst_RV(INS_not, targetReg, op1->TypeGet());
                }
                inst_RV_IV(INS_shr_N, targetReg, (int)op1Size * 8 - 1, op1Size);
                genProduceReg(tree);
                return;
            }
            canReuseFlags = true;
        }

        // We're comparing a register to 0 so we can generate "test reg1, reg1"
        // instead of the longer "cmp reg1, 0"
        ins = INS_test;
        op2 = op1;
    }
    else
    {
        ins = INS_cmp;
    }

    if (type == TYP_UNKNOWN)
    {
        if (op1Type == op2Type)
        {
            type = op1Type;
        }
        else if (genTypeSize(op1Type) == genTypeSize(op2Type))
        {
            // If the types are different but have the same size then we'll use TYP_INT or TYP_LONG.
            // This primarily deals with small type mixes (e.g. byte/ubyte) that need to be widened
            // and compared as int. We should not get long type mixes here but handle that as well
            // just in case.
            type = genTypeSize(op1Type) == 8 ? TYP_LONG : TYP_INT;
        }
        else
        {
            // In the types are different simply use TYP_INT. This deals with small type/int type
            // mixes (e.g. byte/short ubyte/int) that need to be widened and compared as int.
            // Lowering is expected to handle any mixes that involve long types (e.g. int/long).
            type = TYP_INT;
        }

        // The common type cannot be smaller than any of the operand types, we're probably mixing int/long
        assert(genTypeSize(type) >= max(genTypeSize(op1Type), genTypeSize(op2Type)));
        // Small unsigned int types (TYP_BOOL can use anything) should use unsigned comparisons
        assert(!(varTypeIsSmall(type) && varTypeIsUnsigned(type)) || ((tree->gtFlags & GTF_UNSIGNED) != 0));
        // If op1 is smaller then it cannot be in memory, we're probably missing a cast
        assert((genTypeSize(op1Type) >= genTypeSize(type)) || !op1->isUsedFromMemory());
        // If op2 is smaller then it cannot be in memory, we're probably missing a cast
        assert((genTypeSize(op2Type) >= genTypeSize(type)) || !op2->isUsedFromMemory());
        // If we ended up with a small type and op2 is a constant then make sure we don't lose constant bits
        assert(!op2->IsCnsIntOrI() || !varTypeIsSmall(type) || FitsIn(type, op2->AsIntCon()->IconValue()));
    }

    // The type cannot be larger than the machine word size
    assert(genTypeSize(type) <= genTypeSize(TYP_I_IMPL));
    // TYP_UINT and TYP_ULONG should not appear here, only small types can be unsigned
    assert(!varTypeIsUnsigned(type) || varTypeIsSmall(type));

    var_types targetType = tree->TypeGet();

    if (!canReuseFlags || !genCanAvoidEmittingCompareAgainstZero(tree, type))
    {
        // Clear target reg in advance via "xor reg,reg" to avoid movzx after SETCC
        if ((targetReg != REG_NA) && (op1->GetRegNum() != targetReg) && (op2->GetRegNum() != targetReg) &&
            !varTypeIsByte(targetType))
        {
            regMaskTP targetRegMask = genRegMask(targetReg);
            if (((op1->gtGetContainedRegMask() | op2->gtGetContainedRegMask()) & targetRegMask) == 0)
            {
                instGen_Set_Reg_To_Zero(emitTypeSize(TYP_I_IMPL), targetReg);
                targetType = TYP_UBYTE; // just a tip for inst_SETCC that movzx is not needed
            }
        }

        emitAttr size    = emitTypeSize(type);
        bool     canSkip = compiler->opts.OptimizationEnabled() && (ins == INS_cmp) && !op1->isUsedFromMemory() &&
                       !op2->isUsedFromMemory() && emit->IsRedundantCmp(size, op1->GetRegNum(), op2->GetRegNum());

        if (!canSkip)
        {
            emit->emitInsBinary(ins, size, op1, op2);
        }
    }

    // Are we evaluating this into a register?
    if (targetReg != REG_NA)
    {
        inst_SETCC(GenCondition::FromIntegralRelop(tree), targetType, targetReg);
        genProduceReg(tree);
    }
}

//------------------------------------------------------------------------
// genCanAvoidEmittingCompareAgainstZero: A peephole to check if we can avoid
// emitting a compare against zero because the register was previously used
// with an instruction that sets the zero flag.
//
// Parameters:
//    tree   - the compare node
//    opType - type of the compare
//
// Returns:
//    True if the compare can be omitted.
//
bool CodeGen::genCanAvoidEmittingCompareAgainstZero(GenTree* tree, var_types opType)
{
    GenTree* op1 = tree->gtGetOp1();
    assert(tree->gtGetOp2()->IsIntegralConst(0));

    if (!op1->isUsedFromReg())
    {
        return false;
    }

    GenTree*      consumer    = nullptr;
    GenCondition* mutableCond = nullptr;
    GenCondition  cond;

    if (tree->OperIsCompare())
    {
        cond = GenCondition::FromIntegralRelop(tree);
    }
    else
    {
        consumer = genTryFindFlagsConsumer(tree, &mutableCond);
        if (consumer == nullptr)
        {
            return false;
        }

        cond = *mutableCond;
    }

    if (GetEmitter()->AreFlagsSetToZeroCmp(op1->GetRegNum(), emitTypeSize(opType), cond))
    {
        JITDUMP("Not emitting compare due to flags being already set\n");
        return true;
    }

    if ((mutableCond != nullptr) &&
        GetEmitter()->AreFlagsSetForSignJumpOpt(op1->GetRegNum(), emitTypeSize(opType), cond))
    {
        JITDUMP("Not emitting compare due to sign being already set; modifying [%06u] to check sign flag\n",
                Compiler::dspTreeID(consumer));
        *mutableCond =
            (cond.GetCode() == GenCondition::SLT) ? GenCondition(GenCondition::S) : GenCondition(GenCondition::NS);
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// genTryFindFlagsConsumer: Given a node that produces flags, try to look ahead
// for the node that consumes those flags.
//
// Parameters:
//    producer - the node that produces CPU flags
//    cond     - [out] the pointer to the condition inside that consumer.
//
// Returns:
//    A node that consumes the flags, or nullptr if no such node was found.
//
GenTree* CodeGen::genTryFindFlagsConsumer(GenTree* producer, GenCondition** cond)
{
    assert((producer->gtFlags & GTF_SET_FLAGS) != 0);
    // We allow skipping some nodes where we know for sure that the flags are
    // not consumed. In particular we handle resolution nodes. If we see any
    // other node after the compare (which is an uncommon case, happens
    // sometimes with decomposition) then we assume it could consume the flags.
    for (GenTree* candidate = producer->gtNext; candidate != nullptr; candidate = candidate->gtNext)
    {
        if (candidate->OperIs(GT_JCC, GT_SETCC))
        {
            *cond = &candidate->AsCC()->gtCondition;
            return candidate;
        }

        if (candidate->OperIs(GT_SELECTCC))
        {
            *cond = &candidate->AsOpCC()->gtCondition;
            return candidate;
        }

        // The following nodes can be inserted between the compare and the user
        // of the flags by resolution. Codegen for these will never modify CPU
        // flags.
        if (!candidate->OperIs(GT_LCL_VAR, GT_COPY, GT_SWAP))
        {
            // For other nodes we do the conservative thing.
            return nullptr;
        }
    }

    return nullptr;
}

#if !defined(TARGET_64BIT)
//------------------------------------------------------------------------
// genLongToIntCast: Generate code for long to int casts on x86.
//
// Arguments:
//    cast - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    The cast node and its sources (via GT_LONG) must have been assigned registers.
//    The destination cannot be a floating point type or a small integer type.
//
void CodeGen::genLongToIntCast(GenTree* cast)
{
    assert(cast->OperGet() == GT_CAST);

    GenTree* src = cast->gtGetOp1();
    noway_assert(src->OperGet() == GT_LONG);

    genConsumeRegs(src);

    var_types srcType  = ((cast->gtFlags & GTF_UNSIGNED) != 0) ? TYP_ULONG : TYP_LONG;
    var_types dstType  = cast->CastToType();
    regNumber loSrcReg = src->gtGetOp1()->GetRegNum();
    regNumber hiSrcReg = src->gtGetOp2()->GetRegNum();
    regNumber dstReg   = cast->GetRegNum();

    assert((dstType == TYP_INT) || (dstType == TYP_UINT));
    assert(genIsValidIntReg(loSrcReg));
    assert(genIsValidIntReg(hiSrcReg));
    assert(genIsValidIntReg(dstReg));

    if (cast->gtOverflow())
    {
        //
        // Generate an overflow check for [u]long to [u]int casts:
        //
        // long  -> int  - check if the upper 33 bits are all 0 or all 1
        //
        // ulong -> int  - check if the upper 33 bits are all 0
        //
        // long  -> uint - check if the upper 32 bits are all 0
        // ulong -> uint - check if the upper 32 bits are all 0
        //

        if ((srcType == TYP_LONG) && (dstType == TYP_INT))
        {
            BasicBlock* allOne  = genCreateTempLabel();
            BasicBlock* success = genCreateTempLabel();

            inst_RV_RV(INS_test, loSrcReg, loSrcReg, TYP_INT, EA_4BYTE);
            inst_JMP(EJ_js, allOne);

            inst_RV_RV(INS_test, hiSrcReg, hiSrcReg, TYP_INT, EA_4BYTE);
            genJumpToThrowHlpBlk(EJ_jne, SCK_OVERFLOW);
            inst_JMP(EJ_jmp, success);

            genDefineTempLabel(allOne);
            inst_RV_IV(INS_cmp, hiSrcReg, -1, EA_4BYTE);
            genJumpToThrowHlpBlk(EJ_jne, SCK_OVERFLOW);

            genDefineTempLabel(success);
        }
        else
        {
            if ((srcType == TYP_ULONG) && (dstType == TYP_INT))
            {
                inst_RV_RV(INS_test, loSrcReg, loSrcReg, TYP_INT, EA_4BYTE);
                genJumpToThrowHlpBlk(EJ_js, SCK_OVERFLOW);
            }

            inst_RV_RV(INS_test, hiSrcReg, hiSrcReg, TYP_INT, EA_4BYTE);
            genJumpToThrowHlpBlk(EJ_jne, SCK_OVERFLOW);
        }
    }

    inst_Mov(TYP_INT, dstReg, loSrcReg, /* canSkip */ true);

    genProduceReg(cast);
}
#endif

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
            GetEmitter()->emitIns_R_R(INS_test, EA_SIZE(desc.CheckSrcSize()), reg, reg);
            genJumpToThrowHlpBlk(EJ_jl, SCK_OVERFLOW);
            break;

#ifdef TARGET_64BIT
        case GenIntCastDesc::CHECK_UINT_RANGE:
        {
            // We need to check if the value is not greater than 0xFFFFFFFF but this value
            // cannot be encoded in an immediate operand. Use a right shift to test if the
            // upper 32 bits are zero. This requires a temporary register.
            const regNumber tempReg = cast->GetSingleTempReg();
            assert(tempReg != reg);
            GetEmitter()->emitIns_Mov(INS_mov, EA_8BYTE, tempReg, reg, /* canSkip */ false);
            GetEmitter()->emitIns_R_I(INS_shr_N, EA_8BYTE, tempReg, 32);
            genJumpToThrowHlpBlk(EJ_jne, SCK_OVERFLOW);
        }
        break;

        case GenIntCastDesc::CHECK_POSITIVE_INT_RANGE:
            GetEmitter()->emitIns_R_I(INS_cmp, EA_8BYTE, reg, INT32_MAX);
            genJumpToThrowHlpBlk(EJ_ja, SCK_OVERFLOW);
            break;

        case GenIntCastDesc::CHECK_INT_RANGE:
        {
            // Emit "if ((long)(int)x != x) goto OVERFLOW"
            const regNumber regTmp = cast->GetSingleTempReg();
            GetEmitter()->emitIns_Mov(INS_movsxd, EA_8BYTE, regTmp, reg, true);
            GetEmitter()->emitIns_R_R(INS_cmp, EA_8BYTE, reg, regTmp);
            genJumpToThrowHlpBlk(EJ_jne, SCK_OVERFLOW);
        }
        break;
#endif

        default:
        {
            assert(desc.CheckKind() == GenIntCastDesc::CHECK_SMALL_INT_RANGE);
            const int castMaxValue = desc.CheckSmallIntMax();
            const int castMinValue = desc.CheckSmallIntMin();

            GetEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMaxValue);
            genJumpToThrowHlpBlk((castMinValue == 0) ? EJ_ja : EJ_jg, SCK_OVERFLOW);

            if (castMinValue != 0)
            {
                GetEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMinValue);
                genJumpToThrowHlpBlk(EJ_jl, SCK_OVERFLOW);
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
//    Neither the source nor target type can be a floating point type.
//    On x86 casts to (U)BYTE require that the source be in a byte register.
//
void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    genConsumeRegs(cast->CastOp());

    GenTree* const  src    = cast->CastOp();
    const regNumber srcReg = src->isUsedFromReg() ? src->GetRegNum() : REG_NA;
    const regNumber dstReg = cast->GetRegNum();
    emitter*        emit   = GetEmitter();

    assert(genIsValidIntReg(dstReg));

    GenIntCastDesc desc(cast);

    if (desc.CheckKind() != GenIntCastDesc::CHECK_NONE)
    {
        assert(genIsValidIntReg(srcReg));
        genIntCastOverflowCheck(cast, desc, srcReg);
    }

    instruction ins;
    unsigned    insSize;
    bool        canSkip = false;

    switch (desc.ExtendKind())
    {
        case GenIntCastDesc::ZERO_EXTEND_SMALL_INT:
        case GenIntCastDesc::LOAD_ZERO_EXTEND_SMALL_INT:
            ins     = INS_movzx;
            insSize = desc.ExtendSrcSize();
            break;
        case GenIntCastDesc::SIGN_EXTEND_SMALL_INT:
        case GenIntCastDesc::LOAD_SIGN_EXTEND_SMALL_INT:
            ins     = INS_movsx;
            insSize = desc.ExtendSrcSize();
            break;
#ifdef TARGET_64BIT
        case GenIntCastDesc::ZERO_EXTEND_INT:
        case GenIntCastDesc::LOAD_ZERO_EXTEND_INT:
            ins     = INS_mov;
            insSize = 4;
            break;
        case GenIntCastDesc::SIGN_EXTEND_INT:
        case GenIntCastDesc::LOAD_SIGN_EXTEND_INT:
            ins     = INS_movsxd;
            insSize = 4;
            break;
#endif
        case GenIntCastDesc::COPY:
            ins     = INS_mov;
            insSize = desc.ExtendSrcSize();
            canSkip = true;
            break;
        case GenIntCastDesc::LOAD_SOURCE:
            ins     = ins_Load(src->TypeGet());
            insSize = genTypeSize(src);
            break;

        default:
            unreached();
    }

    if (srcReg != REG_NA)
    {
        emit->emitIns_Mov(ins, EA_ATTR(insSize), dstReg, srcReg, canSkip);
    }
    else
    {
        assert(src->isUsedFromMemory());
        inst_RV_TT(ins, EA_ATTR(insSize), dstReg, src);
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
//    The cast is between float and double or vice versa.
//
void CodeGen::genFloatToFloatCast(GenTree* treeNode)
{
    // float <--> double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->GetRegNum();
    assert(genIsValidFloatReg(targetReg));

    GenTree* op1 = treeNode->AsOp()->gtOp1;
#ifdef DEBUG
    // If not contained, must be a valid float reg.
    if (op1->isUsedFromReg())
    {
        assert(genIsValidFloatReg(op1->GetRegNum()));
    }
#endif

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());
    if (srcType == dstType && (op1->isUsedFromReg() && (targetReg == op1->GetRegNum())))
    {
        // source and destinations types are the same and also reside in the same register.
        // we just need to consume and produce the reg in this case.
        ;
    }
    else
    {
        instruction ins = ins_FloatConv(dstType, srcType, emitTypeSize(dstType));

        // integral to floating-point conversions all have RMW semantics if VEX support
        // is not available

        bool isRMW = !compiler->canUseVexEncoding();
        inst_RV_RV_TT(ins, emitTypeSize(dstType), targetReg, targetReg, op1, isRMW, INS_OPTS_NONE);
    }

    genProduceReg(treeNode);
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
void CodeGen::genIntToFloatCast(GenTree* treeNode)
{
    // int type --> float/double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->GetRegNum();
    assert(genIsValidFloatReg(targetReg));

    GenTree* op1 = treeNode->AsOp()->gtOp1;
#ifdef DEBUG
    if (op1->isUsedFromReg())
    {
        assert(genIsValidIntReg(op1->GetRegNum()));
    }
#endif

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(!varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

#if !defined(TARGET_64BIT)
    // We expect morph to replace long to float/double casts with helper calls
    noway_assert(!varTypeIsLong(srcType));
#endif // !defined(TARGET_64BIT)

    // Since xarch emitter doesn't handle reporting gc-info correctly while casting away gc-ness we
    // ensure srcType of a cast is non gc-type.  Codegen should never see BYREF as source type except
    // for GT_LCL_ADDR that represent stack addresses and can be considered as TYP_I_IMPL. In all other
    // cases where src operand is a gc-type and not known to be on stack, Front-end (see fgMorphCast())
    // ensures this by assigning gc-type local to a non gc-type temp and using temp as operand of cast
    // operation.
    if (srcType == TYP_BYREF)
    {
        noway_assert(op1->OperGet() == GT_LCL_ADDR);
        srcType = TYP_I_IMPL;
    }

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (treeNode->gtFlags & GTF_UNSIGNED)
    {
        srcType = varTypeToUnsigned(srcType);
    }

    noway_assert(!varTypeIsGC(srcType));

    // We should never be seeing srcType whose size is not sizeof(int) nor sizeof(long).
    // For conversions from byte/sbyte/int16/uint16 to float/double, we would expect
    // either the front-end or lowering phase to have generated two levels of cast.
    // The first one is for widening smaller int type to int32 and the second one is
    // to the float/double.
    emitAttr srcSize = EA_ATTR(genTypeSize(srcType));
    noway_assert((srcSize == EA_ATTR(genTypeSize(TYP_INT))) || (srcSize == EA_ATTR(genTypeSize(TYP_LONG))));

    // Also we don't expect to see uint32 -> float/double and uint64 -> float conversions
    // here since they should have been lowered appropriately.
    noway_assert(srcType != TYP_UINT);
    assert((srcType != TYP_ULONG) || (dstType != TYP_FLOAT) ||
           compiler->compIsaSupportedDebugOnly(InstructionSet_AVX512F));

    if ((srcType == TYP_ULONG) && varTypeIsFloating(dstType) &&
        compiler->compOpportunisticallyDependsOn(InstructionSet_AVX512F))
    {
        assert(compiler->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
        genConsumeOperands(treeNode->AsOp());
        instruction ins = ins_FloatConv(dstType, srcType, emitTypeSize(srcType));
        GetEmitter()->emitInsBinary(ins, emitTypeSize(srcType), treeNode, op1);
        genProduceReg(treeNode);
        return;
    }

    // To convert int to a float/double, cvtsi2ss/sd SSE2 instruction is used
    // which does a partial write to lower 4/8 bytes of xmm register keeping the other
    // upper bytes unmodified.  If "cvtsi2ss/sd xmmReg, r32/r64" occurs inside a loop,
    // the partial write could introduce a false dependency and could cause a stall
    // if there are further uses of xmmReg. We have such a case occurring with a
    // customer reported version of SpectralNorm benchmark, resulting in 2x perf
    // regression.  To avoid false dependency, we emit "xorps xmmReg, xmmReg" before
    // cvtsi2ss/sd instruction.

    genConsumeOperands(treeNode->AsOp());
    GetEmitter()->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, treeNode->GetRegNum(), treeNode->GetRegNum(),
                                     treeNode->GetRegNum());

    // Note that here we need to specify srcType that will determine
    // the size of source reg/mem operand and rex.w prefix.
    instruction ins = ins_FloatConv(dstType, TYP_INT, emitTypeSize(srcType));

    // integral to floating-point conversions all have RMW semantics if VEX support
    // is not available

    bool isRMW = !compiler->canUseVexEncoding();
    inst_RV_RV_TT(ins, emitTypeSize(srcType), targetReg, targetReg, op1, isRMW, INS_OPTS_NONE);

    // Handle the case of srcType = TYP_ULONG. SSE2 conversion instruction
    // will interpret ULONG value as LONG.  Hence we need to adjust the
    // result if sign-bit of srcType is set.
    if (srcType == TYP_ULONG)
    {
        // The instruction sequence below is less accurate than what clang
        // and gcc generate. However, we keep the current sequence for backward compatibility.
        // If we change the instructions below, FloatingPointUtils::convertUInt64ToDobule
        // should be also updated for consistent conversion result.
        assert(dstType == TYP_DOUBLE);
        assert(op1->isUsedFromReg());

        // Set the flags without modifying op1.
        // test op1Reg, op1Reg
        inst_RV_RV(INS_test, op1->GetRegNum(), op1->GetRegNum(), srcType);

        // No need to adjust result if op1 >= 0 i.e. positive
        // Jge label
        BasicBlock* label = genCreateTempLabel();
        inst_JMP(EJ_jge, label);

        // Adjust the result
        // result = result + 0x43f00000 00000000
        // addsd resultReg,  0x43f00000 00000000
        CORINFO_FIELD_HANDLE* cns = &u8ToDblBitmask;
        if (*cns == nullptr)
        {
            double d;
            static_assert_no_msg(sizeof(double) == sizeof(__int64));
            *((__int64*)&d) = 0x43f0000000000000LL;

            *cns = GetEmitter()->emitFltOrDblConst(d, EA_8BYTE);
        }
        GetEmitter()->emitIns_SIMD_R_R_C(INS_addsd, EA_8BYTE, targetReg, targetReg, *cns, 0);

        genDefineTempLabel(label);
    }

    genProduceReg(treeNode);
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
// TODO-XArch-CQ: (Low-pri) - generate in-line code when DstType = uint64
//
void CodeGen::genFloatToIntCast(GenTree* treeNode)
{
    // we don't expect to see overflow detecting float/double --> int type conversions here
    // as they should have been converted into helper calls by front-end.
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->GetRegNum();
    assert(genIsValidIntReg(targetReg));

    GenTree* op1 = treeNode->AsOp()->gtOp1;
#ifdef DEBUG
    if (op1->isUsedFromReg())
    {
        assert(genIsValidFloatReg(op1->GetRegNum()));
    }
#endif

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && !varTypeIsFloating(dstType));

    // We should never be seeing dstType whose size is neither sizeof(TYP_INT) nor sizeof(TYP_LONG).
    // For conversions to byte/sbyte/int16/uint16 from float/double, we would expect the
    // front-end or lowering phase to have generated two levels of cast. The first one is
    // for float or double to int32/uint32 and the second one for narrowing int32/uint32 to
    // the required smaller int type.
    emitAttr dstSize = EA_ATTR(genTypeSize(dstType));
    noway_assert((dstSize == EA_ATTR(genTypeSize(TYP_INT))) || (dstSize == EA_ATTR(genTypeSize(TYP_LONG))));

    // We shouldn't be seeing uint64 here as it should have been converted
    // into a helper call by either front-end or lowering phase.
    assert(!varTypeIsUnsigned(dstType) || (dstSize != EA_ATTR(genTypeSize(TYP_LONG))));

    // If the dstType is TYP_UINT, we have 32-bits to encode the
    // float number. Any of 33rd or above bits can be the sign bit.
    // To achieve it we pretend as if we are converting it to a long.
    if (varTypeIsUnsigned(dstType) && (dstSize == EA_ATTR(genTypeSize(TYP_INT))))
    {
        dstType = TYP_LONG;
    }

    // Note that we need to specify dstType here so that it will determine
    // the size of destination integer register and also the rex.w prefix.
    genConsumeOperands(treeNode->AsOp());
    instruction ins = ins_FloatConv(TYP_INT, srcType, emitTypeSize(srcType));
    GetEmitter()->emitInsBinary(ins, emitTypeSize(dstType), treeNode, op1);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCkfinite: Generate code for ckfinite opcode.
//
// Arguments:
//    treeNode - The GT_CKFINITE node
//
// Return Value:
//    None.
//
// Assumptions:
//    GT_CKFINITE node has reserved an internal register.
//
// TODO-XArch-CQ - mark the operand as contained if known to be in
// memory (e.g. field or an array element).
//
void CodeGen::genCkfinite(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_CKFINITE);

    GenTree*  op1        = treeNode->AsOp()->gtOp1;
    var_types targetType = treeNode->TypeGet();
    int       expMask    = (targetType == TYP_FLOAT) ? 0x7F800000 : 0x7FF00000; // Bit mask to extract exponent.
    regNumber targetReg  = treeNode->GetRegNum();

    // Extract exponent into a register.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    genConsumeReg(op1);

#ifdef TARGET_64BIT

    // Copy the floating-point value to an integer register. If we copied a float to a long, then
    // right-shift the value so the high 32 bits of the floating-point value sit in the low 32
    // bits of the integer register.
    regNumber srcReg        = op1->GetRegNum();
    var_types targetIntType = ((targetType == TYP_FLOAT) ? TYP_INT : TYP_LONG);
    inst_Mov(targetIntType, tmpReg, srcReg, /* canSkip */ false, emitActualTypeSize(targetType));
    if (targetType == TYP_DOUBLE)
    {
        // right shift by 32 bits to get to exponent.
        inst_RV_SH(INS_shr, EA_8BYTE, tmpReg, 32);
    }

    // Mask exponent with all 1's and check if the exponent is all 1's
    inst_RV_IV(INS_and, tmpReg, expMask, EA_4BYTE);
    inst_RV_IV(INS_cmp, tmpReg, expMask, EA_4BYTE);

    // If exponent is all 1's, throw ArithmeticException
    genJumpToThrowHlpBlk(EJ_je, SCK_ARITH_EXCPN);

    // if it is a finite value copy it to targetReg
    inst_Mov(targetType, targetReg, op1->GetRegNum(), /* canSkip */ true);

#else // !TARGET_64BIT

    // If the target type is TYP_DOUBLE, we want to extract the high 32 bits into the register.
    // There is no easy way to do this. To not require an extra register, we'll use shuffles
    // to move the high 32 bits into the low 32 bits, then shuffle it back, since we
    // need to produce the value into the target register.
    //
    // For TYP_DOUBLE, we'll generate (for targetReg != op1->GetRegNum()):
    //    movaps targetReg, op1->GetRegNum()
    //    shufps targetReg, targetReg, 0xB1    // WZYX => ZWXY
    //    mov_xmm2i tmpReg, targetReg          // tmpReg <= Y
    //    and tmpReg, <mask>
    //    cmp tmpReg, <mask>
    //    je <throw block>
    //    movaps targetReg, op1->GetRegNum()   // copy the value again, instead of un-shuffling it
    //
    // For TYP_DOUBLE with (targetReg == op1->GetRegNum()):
    //    shufps targetReg, targetReg, 0xB1    // WZYX => ZWXY
    //    mov_xmm2i tmpReg, targetReg          // tmpReg <= Y
    //    and tmpReg, <mask>
    //    cmp tmpReg, <mask>
    //    je <throw block>
    //    shufps targetReg, targetReg, 0xB1    // ZWXY => WZYX
    //
    // For TYP_FLOAT, it's the same as TARGET_64BIT:
    //    mov_xmm2i tmpReg, targetReg          // tmpReg <= low 32 bits
    //    and tmpReg, <mask>
    //    cmp tmpReg, <mask>
    //    je <throw block>
    //    movaps targetReg, op1->GetRegNum()      // only if targetReg != op1->GetRegNum()

    regNumber copyToTmpSrcReg; // The register we'll copy to the integer temp.

    if (targetType == TYP_DOUBLE)
    {
        inst_Mov(targetType, targetReg, op1->GetRegNum(), /* canSkip */ true);
        GetEmitter()->emitIns_SIMD_R_R_R_I(INS_shufps, EA_16BYTE, targetReg, targetReg, targetReg, (int8_t)0xB1);
        copyToTmpSrcReg = targetReg;
    }
    else
    {
        copyToTmpSrcReg = op1->GetRegNum();
    }

    // Copy only the low 32 bits. This will be the high order 32 bits of the floating-point
    // value, no matter the floating-point type.
    inst_Mov(TYP_INT, tmpReg, copyToTmpSrcReg, /* canSkip */ false, emitActualTypeSize(TYP_FLOAT));

    // Mask exponent with all 1's and check if the exponent is all 1's
    inst_RV_IV(INS_and, tmpReg, expMask, EA_4BYTE);
    inst_RV_IV(INS_cmp, tmpReg, expMask, EA_4BYTE);

    // If exponent is all 1's, throw ArithmeticException
    genJumpToThrowHlpBlk(EJ_je, SCK_ARITH_EXCPN);

    if ((targetType == TYP_DOUBLE) && (targetReg == op1->GetRegNum()))
    {
        // We need to re-shuffle the targetReg to get the correct result.
        GetEmitter()->emitIns_SIMD_R_R_R_I(INS_shufps, EA_16BYTE, targetReg, targetReg, targetReg, (int8_t)0xB1);
    }
    else
    {
        // In both the TYP_FLOAT and TYP_DOUBLE case, the op1 register is untouched,
        // so copy it to the targetReg. This is faster and smaller for TYP_DOUBLE
        // than re-shuffling the targetReg.
        inst_Mov(targetType, targetReg, op1->GetRegNum(), /* canSkip */ true);
    }

#endif // !TARGET_64BIT

    genProduceReg(treeNode);
}

#ifdef TARGET_AMD64
int CodeGenInterface::genSPtoFPdelta() const
{
    int delta;

#ifdef UNIX_AMD64_ABI

    // We require frame chaining on Unix to support native tool unwinding (such as
    // unwinding by the native debugger). We have a CLR-only extension to the
    // unwind codes (UWOP_SET_FPREG_LARGE) to support SP->FP offsets larger than 240.
    // If Unix ever supports EnC, the RSP == RBP assumption will have to be reevaluated.
    delta = genTotalFrameSize();

#else // !UNIX_AMD64_ABI

    // As per Amd64 ABI, RBP offset from initial RSP can be between 0 and 240 if
    // RBP needs to be reported in unwind codes.  This case would arise for methods
    // with localloc.
    if (compiler->compLocallocUsed)
    {
        // We cannot base delta computation on compLclFrameSize since it changes from
        // tentative to final frame layout and hence there is a possibility of
        // under-estimating offset of vars from FP, which in turn results in under-
        // estimating instruction size.
        //
        // To be predictive and so as never to under-estimate offset of vars from FP
        // we will always position FP at min(240, outgoing arg area size).
        delta = Min(240, (int)compiler->lvaOutgoingArgSpaceSize);
    }
    else if (compiler->opts.compDbgEnC)
    {
        // vm assumption on EnC methods is that rsp and rbp are equal
        delta = 0;
    }
    else
    {
        delta = genTotalFrameSize();
    }

#endif // !UNIX_AMD64_ABI

    return delta;
}

//---------------------------------------------------------------------
// genTotalFrameSize - return the total size of the stack frame, including local size,
// callee-saved register size, etc. For AMD64, this does not include the caller-pushed
// return address.
//
// Return value:
//    Total frame size
//

int CodeGenInterface::genTotalFrameSize() const
{
    assert(!IsUninitialized(compiler->compCalleeRegsPushed));

    int totalFrameSize = compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize;

    assert(totalFrameSize >= 0);
    return totalFrameSize;
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!
//
// We can't compute this directly from the Caller-SP, since the frame pointer
// is based on a maximum delta from Initial-SP, so first we find SP, then
// compute the FP offset.

int CodeGenInterface::genCallerSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    int callerSPtoFPdelta;

    callerSPtoFPdelta = genCallerSPtoInitialSPdelta() + genSPtoFPdelta();

    assert(callerSPtoFPdelta <= 0);
    return callerSPtoFPdelta;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.

int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    int callerSPtoSPdelta = 0;

    callerSPtoSPdelta -= genTotalFrameSize();
    callerSPtoSPdelta -= REGSIZE_BYTES; // caller-pushed return address

    // compCalleeRegsPushed does not account for the frame pointer
    // TODO-Cleanup: shouldn't this be part of genTotalFrameSize?
    if (isFramePointerUsed())
    {
        callerSPtoSPdelta -= REGSIZE_BYTES;
    }

    assert(callerSPtoSPdelta <= 0);
    return callerSPtoSPdelta;
}
#endif // TARGET_AMD64

//-----------------------------------------------------------------------------------------
// genSSE2BitwiseOp - generate SSE2 code for the given oper as "Operand BitWiseOp BitMask"
//
// Arguments:
//    treeNode  - tree node
//
// Return value:
//    None
//
// Assumptions:
//     i) tree oper is one of GT_NEG or GT_INTRINSIC Abs()
//    ii) tree type is floating point type.
//   iii) caller of this routine needs to call genProduceReg()
void CodeGen::genSSE2BitwiseOp(GenTree* treeNode)
{
    regNumber targetReg  = treeNode->GetRegNum();
    regNumber operandReg = genConsumeReg(treeNode->gtGetOp1());
    emitAttr  size       = emitTypeSize(treeNode);

    assert(varTypeIsFloating(treeNode->TypeGet()));
    assert(treeNode->gtGetOp1()->isUsedFromReg());

    CORINFO_FIELD_HANDLE* maskFld = nullptr;
    UINT64                mask    = 0;
    instruction           ins     = INS_invalid;

    if (treeNode->OperIs(GT_NEG))
    {
        // Neg(x) = flip the sign bit.
        // Neg(f) = f ^ 0x80000000 x4 (packed)
        // Neg(d) = d ^ 0x8000000000000000 x2 (packed)
        ins     = INS_xorps;
        mask    = treeNode->TypeIs(TYP_FLOAT) ? 0x8000000080000000UL : 0x8000000000000000UL;
        maskFld = treeNode->TypeIs(TYP_FLOAT) ? &negBitmaskFlt : &negBitmaskDbl;
    }
    else if (treeNode->OperIs(GT_INTRINSIC))
    {
        assert(treeNode->AsIntrinsic()->gtIntrinsicName == NI_System_Math_Abs);
        // Abs(x) = set sign-bit to zero
        // Abs(f) = f & 0x7fffffff x4 (packed)
        // Abs(d) = d & 0x7fffffffffffffff x2 (packed)
        ins     = INS_andps;
        mask    = treeNode->TypeIs(TYP_FLOAT) ? 0x7FFFFFFF7FFFFFFFUL : 0x7FFFFFFFFFFFFFFFUL;
        maskFld = treeNode->TypeIs(TYP_FLOAT) ? &absBitmaskFlt : &absBitmaskDbl;
    }
    else
    {
        assert(!"genSSE2BitwiseOp: unsupported oper");
    }

    if (*maskFld == nullptr)
    {
        simd16_t constValue;

        constValue.u64[0] = mask;
        constValue.u64[1] = mask;

#if defined(FEATURE_SIMD)
        *maskFld = GetEmitter()->emitSimd16Const(constValue);
#else
        *maskFld = GetEmitter()->emitBlkConst(&constValue, 16, 16, treeNode->TypeGet());
#endif
    }

    GetEmitter()->emitIns_SIMD_R_R_C(ins, EA_16BYTE, targetReg, operandReg, *maskFld, 0);
}

//-----------------------------------------------------------------------------------------
// genSSE41RoundOp - generate SSE41 code for the given tree as a round operation
//
// Arguments:
//    treeNode  - tree node
//
// Return value:
//    None
//
// Assumptions:
//     i) SSE4.1 is supported by the underlying hardware
//    ii) treeNode oper is a GT_INTRINSIC
//   iii) treeNode type is a floating point type
//    iv) treeNode is not used from memory
//     v) tree oper is NI_System_Math{F}_Round, _Ceiling, _Floor, or _Truncate
//    vi) caller of this routine needs to call genProduceReg()
void CodeGen::genSSE41RoundOp(GenTreeOp* treeNode)
{
    // i) SSE4.1 is supported by the underlying hardware
    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_SSE41));

    // ii) treeNode oper is a GT_INTRINSIC
    assert(treeNode->OperGet() == GT_INTRINSIC);

    GenTree* srcNode = treeNode->gtGetOp1();

    // iii) treeNode type is floating point type
    assert(varTypeIsFloating(srcNode));
    assert(srcNode->TypeGet() == treeNode->TypeGet());

    // iv) treeNode is not used from memory
    assert(!treeNode->isUsedFromMemory());

    genConsumeOperands(treeNode);

    instruction ins  = (treeNode->TypeGet() == TYP_FLOAT) ? INS_roundss : INS_roundsd;
    emitAttr    size = emitTypeSize(treeNode);

    regNumber dstReg = treeNode->GetRegNum();

    int8_t ival = 0;

    // v) tree oper is NI_System_Math{F}_Round, _Ceiling, _Floor, or _Truncate
    switch (treeNode->AsIntrinsic()->gtIntrinsicName)
    {
        case NI_System_Math_Round:
            ival = 4;
            break;

        case NI_System_Math_Ceiling:
            ival = 10;
            break;

        case NI_System_Math_Floor:
            ival = 9;
            break;

        case NI_System_Math_Truncate:
            ival = 11;
            break;

        default:
            ins = INS_invalid;
            assert(!"genSSE41RoundOp: unsupported intrinsic");
            unreached();
    }

    bool isRMW = !compiler->canUseVexEncoding();
    inst_RV_RV_TT_IV(ins, size, dstReg, dstReg, srcNode, ival, isRMW);
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
    // Handle intrinsics that can be implemented by target-specific instructions
    switch (treeNode->gtIntrinsicName)
    {
        case NI_System_Math_Abs:
            genSSE2BitwiseOp(treeNode);
            break;

        case NI_System_Math_Ceiling:
        case NI_System_Math_Floor:
        case NI_System_Math_Truncate:
        case NI_System_Math_Round:
            genSSE41RoundOp(treeNode->AsOp());
            break;

        case NI_System_Math_Sqrt:
        {
            // Both operand and its result must be of the same floating point type.
            GenTree* srcNode = treeNode->gtGetOp1();
            assert(varTypeIsFloating(srcNode));
            assert(srcNode->TypeGet() == treeNode->TypeGet());

            genConsumeOperands(treeNode->AsOp());

            const instruction ins = (treeNode->TypeGet() == TYP_FLOAT) ? INS_sqrtss : INS_sqrtsd;

            regNumber targetReg = treeNode->GetRegNum();
            bool      isRMW     = !compiler->canUseVexEncoding();

            inst_RV_RV_TT(ins, emitTypeSize(treeNode), targetReg, targetReg, srcNode, isRMW, INS_OPTS_NONE);
            break;
        }

#if defined(FEATURE_SIMD)
        // The handling is a bit more complex so genSimdUpperSave/Restore
        // handles genConsumeOperands and genProduceReg

        case NI_SIMD_UpperRestore:
        {
            genSimdUpperRestore(treeNode);
            return;
        }

        case NI_SIMD_UpperSave:
        {
            genSimdUpperSave(treeNode);
            return;
        }
#endif // FEATURE_SIMD

        default:
            assert(!"genIntrinsic: Unsupported intrinsic");
            unreached();
    }

    genProduceReg(treeNode);
}

//-------------------------------------------------------------------------- //
// getBaseVarForPutArgStk - returns the baseVarNum for passing a stack arg.
//
// Arguments
//    treeNode - the GT_PUTARG_STK node
//
// Return value:
//    The number of the base variable.
//
// Note:
//    If tail call the outgoing args are placed in the caller's incoming arg stack space.
//    Otherwise, they go in the outgoing arg area on the current frame.
//
//    On Windows the caller always creates slots (homing space) in its frame for the
//    first 4 arguments of a callee (register passed args). So, the baseVarNum is always 0.
//    For System V systems there is no such calling convention requirement, and the code needs to find
//    the first stack passed argument from the caller. This is done by iterating over
//    all the lvParam variables and finding the first with GetArgReg() equals to REG_STK.
//
unsigned CodeGen::getBaseVarForPutArgStk(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_PUTARG_STK);

    unsigned baseVarNum;

    // Whether to setup stk arg in incoming or out-going arg area?
    // Fast tail calls implemented as epilog+jmp = stk arg is setup in incoming arg area.
    // All other calls - stk arg is setup in out-going arg area.
    if (treeNode->AsPutArgStk()->putInIncomingArgArea())
    {
        // See the note in the function header re: finding the first stack passed argument.
        baseVarNum = getFirstArgWithStackSlot();
        assert(baseVarNum != BAD_VAR_NUM);

#ifdef DEBUG
        // This must be a fast tail call.
        assert(treeNode->AsPutArgStk()->gtCall->AsCall()->IsFastTailCall());

        // Since it is a fast tail call, the existence of first incoming arg is guaranteed
        // because fast tail call requires that in-coming arg area of caller is >= out-going
        // arg area required for tail call.
        LclVarDsc* varDsc = compiler->lvaGetDesc(baseVarNum);
        assert(varDsc != nullptr);

#ifdef UNIX_AMD64_ABI
        assert(!varDsc->lvIsRegArg && varDsc->GetArgReg() == REG_STK);
#else  // !UNIX_AMD64_ABI
        // On Windows this assert is always true. The first argument will always be in REG_ARG_0 or REG_FLTARG_0.
        assert(varDsc->lvIsRegArg && (varDsc->GetArgReg() == REG_ARG_0 || varDsc->GetArgReg() == REG_FLTARG_0));
#endif // !UNIX_AMD64_ABI
#endif // !DEBUG
    }
    else
    {
#if FEATURE_FIXED_OUT_ARGS
        baseVarNum = compiler->lvaOutgoingArgSpaceVar;
#else  // !FEATURE_FIXED_OUT_ARGS
        assert(!"No BaseVarForPutArgStk on x86");
        baseVarNum = BAD_VAR_NUM;
#endif // !FEATURE_FIXED_OUT_ARGS
    }

    return baseVarNum;
}

//---------------------------------------------------------------------
// genAlignStackBeforeCall: Align the stack if necessary before a call.
//
// Arguments:
//    putArgStk - the putArgStk node.
//
void CodeGen::genAlignStackBeforeCall(GenTreePutArgStk* putArgStk)
{
#if defined(UNIX_X86_ABI)

    genAlignStackBeforeCall(putArgStk->gtCall);

#endif // UNIX_X86_ABI
}

//---------------------------------------------------------------------
// genAlignStackBeforeCall: Align the stack if necessary before a call.
//
// Arguments:
//    call - the call node.
//
void CodeGen::genAlignStackBeforeCall(GenTreeCall* call)
{
#if defined(UNIX_X86_ABI)

    // Have we aligned the stack yet?
    if (!call->gtArgs.IsStkAlignmentDone())
    {
        // We haven't done any stack alignment yet for this call.  We might need to create
        // an alignment adjustment, even if this function itself doesn't have any stack args.
        // This can happen if this function call is part of a nested call sequence, and the outer
        // call has already pushed some arguments.

        unsigned stkLevel = genStackLevel + call->gtArgs.GetStkSizeBytes();
        call->gtArgs.ComputeStackAlignment(stkLevel);

        unsigned padStkAlign = call->gtArgs.GetStkAlign();
        if (padStkAlign != 0)
        {
            // Now generate the alignment
            inst_RV_IV(INS_sub, REG_SPBASE, padStkAlign, EA_PTRSIZE);
            AddStackLevel(padStkAlign);
            AddNestedAlignment(padStkAlign);
        }

        call->gtArgs.SetStkAlignmentDone();
    }

#endif // UNIX_X86_ABI
}

//---------------------------------------------------------------------
// genRemoveAlignmentAfterCall: After a call, remove the alignment
// added before the call, if any.
//
// Arguments:
//    call - the call node.
//    bias - additional stack adjustment
//
// Note:
//    When bias > 0, caller should adjust stack level appropriately as
//    bias is not considered when adjusting stack level.
//
void CodeGen::genRemoveAlignmentAfterCall(GenTreeCall* call, unsigned bias)
{
#if defined(TARGET_X86)
#if defined(UNIX_X86_ABI)
    // Put back the stack pointer if there was any padding for stack alignment
    unsigned padStkAlign  = call->gtArgs.GetStkAlign();
    unsigned padStkAdjust = padStkAlign + bias;

    if (padStkAdjust != 0)
    {
        inst_RV_IV(INS_add, REG_SPBASE, padStkAdjust, EA_PTRSIZE);
        SubtractStackLevel(padStkAlign);
        SubtractNestedAlignment(padStkAlign);
    }
#else  // UNIX_X86_ABI
    if (bias != 0)
    {
        if (bias == sizeof(int))
        {
            inst_RV(INS_pop, REG_ECX, TYP_INT);
        }
        else
        {
            inst_RV_IV(INS_add, REG_SPBASE, bias, EA_PTRSIZE);
        }
    }
#endif // !UNIX_X86_ABI_
#else  // TARGET_X86
    assert(bias == 0);
#endif // !TARGET_X86
}

#ifdef TARGET_X86

//---------------------------------------------------------------------
// genAdjustStackForPutArgStk:
//    adjust the stack pointer for a putArgStk node if necessary.
//
// Arguments:
//    putArgStk - the putArgStk node.
//
// Returns: true if the stack pointer was adjusted; false otherwise.
//
// Notes:
//    Sets `m_pushStkArg` to true if the stack arg needs to be pushed,
//    false if the stack arg needs to be stored at the current stack
//    pointer address. This is exactly the opposite of the return value
//    of this function.
//
bool CodeGen::genAdjustStackForPutArgStk(GenTreePutArgStk* putArgStk)
{
    const unsigned argSize = putArgStk->GetStackByteSize();
    GenTree*       source  = putArgStk->gtGetOp1();

#ifdef FEATURE_SIMD
    if (!source->OperIs(GT_FIELD_LIST) && varTypeIsSIMD(source))
    {
        inst_RV_IV(INS_sub, REG_SPBASE, argSize, EA_PTRSIZE);
        AddStackLevel(argSize);
        m_pushStkArg = false;
        return true;
    }
#endif // FEATURE_SIMD

#ifdef DEBUG
    switch (putArgStk->gtPutArgStkKind)
    {
        case GenTreePutArgStk::Kind::RepInstr:
        case GenTreePutArgStk::Kind::Unroll:
            assert(!source->GetLayout(compiler)->HasGCPtr());
            break;

        case GenTreePutArgStk::Kind::Push:
            assert(source->OperIs(GT_FIELD_LIST) || source->GetLayout(compiler)->HasGCPtr() ||
                   (argSize < XMM_REGSIZE_BYTES));
            break;

        default:
            unreached();
    }
#endif // DEBUG

    // In lowering (see "LowerPutArgStk") we have determined what sort of instructions
    // are going to be used for this node. If we'll not be using "push"es, the stack
    // needs to be adjusted first (s. t. the SP points to the base of the outgoing arg).
    //
    if (!putArgStk->isPushKind())
    {
        // If argSize is large, we need to probe the stack like we do in the prolog (genAllocLclFrame)
        // or for localloc (genLclHeap), to ensure we touch the stack pages sequentially, and don't miss
        // the stack guard pages. The prolog probes, but we don't know at this point how much higher
        // the last probed stack pointer value is. We default a threshold. Any size below this threshold
        // we are guaranteed the stack has been probed. Above this threshold, we don't know. The threshold
        // should be high enough to cover all common cases. Increasing the threshold means adding a few
        // more "lowest address of stack" probes in the prolog. Since this is relatively rare, add it to
        // stress modes.

        if ((argSize >= ARG_STACK_PROBE_THRESHOLD_BYTES) ||
            compiler->compStressCompile(Compiler::STRESS_GENERIC_VARN, 5))
        {
            genStackPointerConstantAdjustmentLoopWithProbe(-(ssize_t)argSize, /* trackSpAdjustments */ true);
        }
        else
        {
            inst_RV_IV(INS_sub, REG_SPBASE, argSize, EA_PTRSIZE);
        }

        AddStackLevel(argSize);
        m_pushStkArg = false;
        return true;
    }

    // Otherwise, "push" will be adjusting the stack for us.
    m_pushStkArg = true;
    return false;
}

//---------------------------------------------------------------------
// genPutArgStkFieldList - generate code for passing a GT_FIELD_LIST arg on the stack.
//
// Arguments
//    treeNode      - the GT_PUTARG_STK node whose op1 is a GT_FIELD_LIST
//
// Return value:
//    None
//
void CodeGen::genPutArgStkFieldList(GenTreePutArgStk* putArgStk)
{
    GenTreeFieldList* const fieldList = putArgStk->gtOp1->AsFieldList();
    assert(fieldList != nullptr);

    // Set m_pushStkArg and pre-adjust the stack if necessary.
    const bool preAdjustedStack = genAdjustStackForPutArgStk(putArgStk);

    // For now, we only support the "push" case; we will push a full slot for the first field of each slot
    // within the struct.
    assert(putArgStk->isPushKind() && !preAdjustedStack && m_pushStkArg);

    // If we have pre-adjusted the stack and are simply storing the fields in order, set the offset to 0.
    // (Note that this mode is not currently being used.)
    // If we are pushing the arguments (i.e. we have not pre-adjusted the stack), then we are pushing them
    // in reverse order, so we start with the current field offset at the size of the struct arg (which must be
    // a multiple of the target pointer size).
    unsigned  currentOffset   = (preAdjustedStack) ? 0 : putArgStk->GetStackByteSize();
    unsigned  prevFieldOffset = currentOffset;
    regNumber intTmpReg       = REG_NA;
    regNumber simdTmpReg      = REG_NA;
    if (putArgStk->AvailableTempRegCount() != 0)
    {
        regMaskTP rsvdRegs = putArgStk->gtRsvdRegs;
        if ((rsvdRegs & RBM_ALLINT) != 0)
        {
            intTmpReg = putArgStk->GetSingleTempReg(RBM_ALLINT);
            assert(genIsValidIntReg(intTmpReg));
        }
        if ((rsvdRegs & RBM_ALLFLOAT) != 0)
        {
            simdTmpReg = putArgStk->GetSingleTempReg(RBM_ALLFLOAT);
            assert(genIsValidFloatReg(simdTmpReg));
        }
        assert(genCountBits(rsvdRegs) == (unsigned)((intTmpReg == REG_NA) ? 0 : 1) + ((simdTmpReg == REG_NA) ? 0 : 1));
    }

    for (GenTreeFieldList::Use& use : fieldList->Uses())
    {
        GenTree* const  fieldNode   = use.GetNode();
        const unsigned  fieldOffset = use.GetOffset();
        const var_types fieldType   = use.GetType();

        // Long-typed nodes should have been handled by the decomposition pass, and lowering should have sorted the
        // field list in descending order by offset.
        assert(!varTypeIsLong(fieldType));
        assert(fieldOffset <= prevFieldOffset);

        // Consume the register, if any, for this field. Note that genConsumeRegs() will appropriately
        // update the liveness info for a lclVar that has been marked RegOptional, which hasn't been
        // assigned a register, and which is therefore contained.
        // Unlike genConsumeReg(), it handles the case where no registers are being consumed.
        genConsumeRegs(fieldNode);
        regNumber argReg = fieldNode->isUsedFromSpillTemp() ? REG_NA : fieldNode->GetRegNum();

        // If the field is slot-like, we can use a push instruction to store the entire register no matter the type.
        //
        // The GC encoder requires that the stack remain 4-byte aligned at all times. Round the adjustment up
        // to the next multiple of 4. If we are going to generate a `push` instruction, the adjustment must
        // not require rounding.
        const bool fieldIsSlot = ((fieldOffset % 4) == 0) && ((prevFieldOffset - fieldOffset) >= 4);
        int        adjustment  = roundUp(currentOffset - fieldOffset, 4);
        if (fieldIsSlot && !varTypeIsSIMD(fieldType))
        {
            unsigned pushSize = genTypeSize(genActualType(fieldType));
            assert((pushSize % 4) == 0);
            adjustment -= pushSize;
            while (adjustment != 0)
            {
                inst_IV(INS_push, 0);
                currentOffset -= pushSize;
                AddStackLevel(pushSize);
                adjustment -= pushSize;
            }

            m_pushStkArg = true;
        }
        else
        {
            m_pushStkArg = false;

            // We always "push" floating point fields (i.e. they are full slot values that don't
            // require special handling).
            assert(varTypeIsIntegralOrI(fieldNode) || varTypeIsSIMD(fieldNode));

            // If we can't push this field, it needs to be in a register so that we can store
            // it to the stack location.
            if (adjustment != 0)
            {
                // This moves the stack pointer to fieldOffset.
                // For this case, we must adjust the stack and generate stack-relative stores rather than pushes.
                // Adjust the stack pointer to the next slot boundary.
                inst_RV_IV(INS_sub, REG_SPBASE, adjustment, EA_PTRSIZE);
                currentOffset -= adjustment;
                AddStackLevel(adjustment);
            }

            // Does it need to be in a byte register?
            // If so, we'll use intTmpReg, which must have been allocated as a byte register.
            // If it's already in a register, but not a byteable one, then move it.
            if (varTypeIsByte(fieldType) && ((argReg == REG_NA) || ((genRegMask(argReg) & RBM_BYTE_REGS) == 0)))
            {
                assert(intTmpReg != REG_NA);
                noway_assert((genRegMask(intTmpReg) & RBM_BYTE_REGS) != 0);
                if (argReg != REG_NA)
                {
                    inst_Mov(fieldType, intTmpReg, argReg, /* canSkip */ false);
                    argReg = intTmpReg;
                }
            }
        }

        bool canStoreFullSlot = fieldIsSlot;
        bool canLoadFullSlot  = genIsValidIntReg(argReg);
        if (argReg == REG_NA)
        {
            assert((genTypeSize(fieldNode) <= TARGET_POINTER_SIZE));
            assert(genTypeSize(genActualType(fieldNode)) == genTypeSize(genActualType(fieldType)));

            // We can widen local loads if the excess only affects padding bits.
            canLoadFullSlot = (genTypeSize(fieldNode) == TARGET_POINTER_SIZE) || fieldNode->isUsedFromSpillTemp() ||
                              (fieldNode->OperIsLocalRead() && (genTypeSize(fieldNode) >= genTypeSize(fieldType)));
        }

        if (canStoreFullSlot && canLoadFullSlot)
        {
            assert(m_pushStkArg);
            assert(genTypeSize(fieldNode) <= TARGET_POINTER_SIZE);
            inst_TT(INS_push, emitActualTypeSize(fieldNode), fieldNode);

            currentOffset -= TARGET_POINTER_SIZE;
            AddStackLevel(TARGET_POINTER_SIZE);
        }
        else
        {
            // If the field is of GC type, we must use a push instruction, since the emitter is not
            // otherwise able to detect stores into the outgoing argument area of the stack on x86.
            assert(!varTypeIsGC(fieldNode));

            // First, if needed, load the field into the temporary register.
            if (argReg == REG_NA)
            {
                assert(varTypeIsIntegralOrI(fieldNode) && genIsValidIntReg(intTmpReg));

                if (fieldNode->isContainedIntOrIImmed())
                {
                    genSetRegToConst(intTmpReg, fieldNode->TypeGet(), fieldNode);
                }
                else
                {
                    // Use the smaller "mov" instruction in case we do not need a sign/zero-extending load.
                    instruction loadIns  = canLoadFullSlot ? INS_mov : ins_Load(fieldNode->TypeGet());
                    emitAttr    loadSize = canLoadFullSlot ? EA_PTRSIZE : emitTypeSize(fieldNode);
                    inst_RV_TT(loadIns, loadSize, intTmpReg, fieldNode);
                }

                argReg = intTmpReg;
            }

#if defined(FEATURE_SIMD)
            if (fieldType == TYP_SIMD12)
            {
                assert(genIsValidFloatReg(simdTmpReg));
                genStoreSimd12ToStack(argReg, simdTmpReg);
            }
            else
#endif // defined(FEATURE_SIMD)
            {
                // Using wide stores here avoids having to reserve a byteable register when we could not
                // use "push" due to the field node being an indirection (i. e. for "!canLoadFullSlot").
                var_types storeType = canStoreFullSlot ? genActualType(fieldType) : fieldType;
                genStoreRegToStackArg(storeType, argReg, fieldOffset - currentOffset);
            }

            if (m_pushStkArg)
            {
                // We always push a slot-rounded size.
                currentOffset -= roundUp(genTypeSize(fieldType), TARGET_POINTER_SIZE);
            }
        }

        prevFieldOffset = fieldOffset;
    }

    if (currentOffset != 0)
    {
        // We don't expect padding at the beginning of a struct, but it could happen with explicit layout.
        inst_RV_IV(INS_sub, REG_SPBASE, currentOffset, EA_PTRSIZE);
        AddStackLevel(currentOffset);
    }
}
#endif // TARGET_X86

//---------------------------------------------------------------------
// genPutArgStk - generate code for passing an arg on the stack.
//
// Arguments
//    treeNode      - the GT_PUTARG_STK node
//
void CodeGen::genPutArgStk(GenTreePutArgStk* putArgStk)
{
    GenTree*  data       = putArgStk->gtOp1;
    var_types targetType = genActualType(data->TypeGet());

#ifdef TARGET_X86

    // On a 32-bit target, all of the long arguments are handled with GT_FIELD_LISTs of TYP_INT.
    assert(targetType != TYP_LONG);
    assert((putArgStk->GetStackByteSize() % TARGET_POINTER_SIZE) == 0);

    genAlignStackBeforeCall(putArgStk);

    if (data->OperIs(GT_FIELD_LIST))
    {
        genPutArgStkFieldList(putArgStk);
        return;
    }

    if (varTypeIsStruct(targetType))
    {
        genAdjustStackForPutArgStk(putArgStk);
        genPutStructArgStk(putArgStk);
        return;
    }

    genConsumeRegs(data);

    if (data->isUsedFromReg())
    {
        genPushReg(targetType, data->GetRegNum());
    }
    else
    {
        assert(genTypeSize(data) == TARGET_POINTER_SIZE);
        inst_TT(INS_push, emitTypeSize(data), data);
        AddStackLevel(TARGET_POINTER_SIZE);
    }
#else // !TARGET_X86
    {
        unsigned baseVarNum = getBaseVarForPutArgStk(putArgStk);

#ifdef UNIX_AMD64_ABI

        if (data->OperIs(GT_FIELD_LIST))
        {
            genPutArgStkFieldList(putArgStk, baseVarNum);
            return;
        }
        else if (varTypeIsStruct(targetType))
        {
            m_stkArgVarNum = baseVarNum;
            m_stkArgOffset = putArgStk->getArgOffset();
            genPutStructArgStk(putArgStk);
            m_stkArgVarNum = BAD_VAR_NUM;
            return;
        }
#endif // UNIX_AMD64_ABI

        noway_assert(targetType != TYP_STRUCT);

        // Get argument offset on stack.
        // Here we cross check that argument offset hasn't changed from lowering to codegen since
        // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
        unsigned argOffset = putArgStk->getArgOffset();

#ifdef DEBUG
        CallArg* callArg   = putArgStk->gtCall->gtArgs.FindByNode(putArgStk);
        assert(callArg != nullptr);
        assert(argOffset == callArg->AbiInfo.ByteOffset);
#endif

        if (data->isContainedIntOrIImmed())
        {
            GetEmitter()->emitIns_S_I(ins_Store(targetType), emitTypeSize(targetType), baseVarNum, argOffset,
                                      (int)data->AsIntConCommon()->IconValue());
        }
        else
        {
            assert(data->isUsedFromReg());
            genConsumeReg(data);
            GetEmitter()->emitIns_S_R(ins_Store(targetType), emitTypeSize(targetType), data->GetRegNum(), baseVarNum,
                                      argOffset);
        }
    }
#endif // !TARGET_X86
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

#ifndef UNIX_AMD64_ABI
    assert(targetType != TYP_STRUCT);
#endif // !UNIX_AMD64_ABI

    GenTree* op1 = tree->gtOp1;
    genConsumeReg(op1);

    // If child node is not already in the register we need, move it
    inst_Mov(targetType, targetReg, op1->GetRegNum(), /* canSkip */ true);

    genProduceReg(tree);
}

#ifdef TARGET_X86
// genPushReg: Push a register value onto the stack and adjust the stack level
//
// Arguments:
//    type   - the type of value to be stored
//    reg    - the register containing the value
//
// Notes:
//    For TYP_LONG, the srcReg must be a floating point register.
//    Otherwise, the register type must be consistent with the given type.
//
void CodeGen::genPushReg(var_types type, regNumber srcReg)
{
    unsigned size = genTypeSize(type);
    if (varTypeIsIntegralOrI(type) && type != TYP_LONG)
    {
        assert(genIsValidIntReg(srcReg));
        inst_RV(INS_push, srcReg, type);
    }
    else
    {
        instruction ins;
        emitAttr    attr = emitTypeSize(type);
        if (type == TYP_LONG)
        {
            // On x86, the only way we can push a TYP_LONG from a register is if it is in an xmm reg.
            // This is only used when we are pushing a struct from memory to memory, and basically is
            // handling an 8-byte "chunk", as opposed to strictly a long type.
            ins = INS_movq;
        }
        else
        {
            ins = ins_Store(type);
        }
        assert(genIsValidFloatReg(srcReg));
        inst_RV_IV(INS_sub, REG_SPBASE, size, EA_PTRSIZE);
        GetEmitter()->emitIns_AR_R(ins, attr, srcReg, REG_SPBASE, 0);
    }
    AddStackLevel(size);
}
#endif // TARGET_X86

#if defined(FEATURE_PUT_STRUCT_ARG_STK)
// genStoreRegToStackArg: Store a register value into the stack argument area
//
// Arguments:
//    type   - the type of value to be stored
//    reg    - the register containing the value
//    offset - the offset from the base (see Assumptions below)
//
// Notes:
//    A type of TYP_STRUCT instructs this method to store a 16-byte chunk
//    at the given offset (i.e. not the full struct).
//
// Assumptions:
//    The caller must set the context appropriately before calling this method:
//    - On x64, m_stkArgVarNum must be set according to whether this is a regular or tail call.
//    - On x86, the caller must set m_pushStkArg if this method should push the argument.
//      Otherwise, the argument is stored at the given offset from sp.
//
// TODO: In the below code the load and store instructions are for 16 bytes, but the
//          type is EA_8BYTE. The movdqa/u are 16 byte instructions, so it works, but
//          this probably needs to be changed.
//
void CodeGen::genStoreRegToStackArg(var_types type, regNumber srcReg, int offset)
{
    assert(srcReg != REG_NA);
    instruction ins;
    emitAttr    attr;
    unsigned    size;

    if (type == TYP_STRUCT)
    {
        ins = INS_movdqu;
        // This should be changed!
        attr = EA_16BYTE;
        size = 16;
    }
    else
    {
#ifdef FEATURE_SIMD
        if (varTypeIsSIMD(type))
        {
            assert(genIsValidFloatReg(srcReg));
            ins = ins_Store(type); // TODO-CQ: pass 'aligned' correctly
        }
        else
#endif // FEATURE_SIMD
#ifdef TARGET_X86
            if (type == TYP_LONG)
        {
            assert(genIsValidFloatReg(srcReg));
            ins = INS_movq;
        }
        else
#endif // TARGET_X86
        {
            assert((varTypeUsesFloatReg(type) && genIsValidFloatReg(srcReg)) ||
                   (varTypeUsesIntReg(type) && genIsValidIntReg(srcReg)));
            ins = ins_Store(type);
        }
        attr = emitTypeSize(type);
        size = genTypeSize(type);
    }

#ifdef TARGET_X86
    if (m_pushStkArg)
    {
        genPushReg(type, srcReg);
    }
    else
    {
        GetEmitter()->emitIns_AR_R(ins, attr, srcReg, REG_SPBASE, offset);
    }
#else  // !TARGET_X86
    assert(m_stkArgVarNum != BAD_VAR_NUM);
    GetEmitter()->emitIns_S_R(ins, attr, srcReg, m_stkArgVarNum, m_stkArgOffset + offset);
#endif // !TARGET_X86
}

//---------------------------------------------------------------------
// genPutStructArgStk - generate code for copying a struct arg on the stack by value.
//                In case there are references to heap object in the struct,
//                it generates the gcinfo as well.
//
// Arguments
//    putArgStk - the GT_PUTARG_STK node
//
// Notes:
//    In the case of fixed out args, the caller must have set m_stkArgVarNum to the variable number
//    corresponding to the argument area (where we will put the argument on the stack).
//    For tail calls this is the baseVarNum = 0.
//    For non tail calls this is the outgoingArgSpace.
//
void CodeGen::genPutStructArgStk(GenTreePutArgStk* putArgStk)
{
    GenTree*  source     = putArgStk->gtGetOp1();
    var_types targetType = source->TypeGet();

#if defined(TARGET_X86) && defined(FEATURE_SIMD)
    if (putArgStk->isSIMD12())
    {
        genPutArgStkSimd12(putArgStk);
        return;
    }
#endif // defined(TARGET_X86) && defined(FEATURE_SIMD)

    if (varTypeIsSIMD(targetType))
    {
        regNumber srcReg = genConsumeReg(source);
        assert((srcReg != REG_NA) && (genIsValidFloatReg(srcReg)));
        genStoreRegToStackArg(targetType, srcReg, 0);
        return;
    }

    assert(targetType == TYP_STRUCT);

    switch (putArgStk->gtPutArgStkKind)
    {
        case GenTreePutArgStk::Kind::RepInstr:
            genStructPutArgRepMovs(putArgStk);
            break;
#ifndef TARGET_X86
        case GenTreePutArgStk::Kind::PartialRepInstr:
            genStructPutArgPartialRepMovs(putArgStk);
            break;
#endif // !TARGET_X86

        case GenTreePutArgStk::Kind::Unroll:
            genStructPutArgUnroll(putArgStk);
            break;

#ifdef TARGET_X86
        case GenTreePutArgStk::Kind::Push:
            genStructPutArgPush(putArgStk);
            break;
#endif // TARGET_X86

        default:
            unreached();
    }
}
#endif // defined(FEATURE_PUT_STRUCT_ARG_STK)

/*****************************************************************************
 *
 *  Create and record GC Info for the function.
 */
#ifndef JIT32_GCENCODER
void
#else  // !JIT32_GCENCODER
void*
#endif // !JIT32_GCENCODER
CodeGen::genCreateAndStoreGCInfo(unsigned codeSize, unsigned prologSize, unsigned epilogSize DEBUGARG(void* codePtr))
{
#ifdef JIT32_GCENCODER
    return genCreateAndStoreGCInfoJIT32(codeSize, prologSize, epilogSize DEBUGARG(codePtr));
#else  // !JIT32_GCENCODER
    genCreateAndStoreGCInfoX64(codeSize, prologSize DEBUGARG(codePtr));
#endif // !JIT32_GCENCODER
}

#ifdef JIT32_GCENCODER
void* CodeGen::genCreateAndStoreGCInfoJIT32(unsigned codeSize,
                                            unsigned prologSize,
                                            unsigned epilogSize DEBUGARG(void* codePtr))
{
    BYTE    headerBuf[64];
    InfoHdr header;

    int s_cached;

#ifdef FEATURE_EH_FUNCLETS
    // We should do this before gcInfoBlockHdrSave since varPtrTableSize must be finalized before it
    if (compiler->ehAnyFunclets())
    {
        gcInfo.gcMarkFilterVarsPinned();
    }
#endif

#ifdef DEBUG
    size_t headerSize =
#endif
        compiler->compInfoBlkSize =
            gcInfo.gcInfoBlockHdrSave(headerBuf, 0, codeSize, prologSize, epilogSize, &header, &s_cached);

    size_t argTabOffset = 0;
    size_t ptrMapSize   = gcInfo.gcPtrTableSize(header, codeSize, &argTabOffset);

#if DISPLAY_SIZES

    if (GetInterruptible())
    {
        gcHeaderISize += compiler->compInfoBlkSize;
        gcPtrMapISize += ptrMapSize;
    }
    else
    {
        gcHeaderNSize += compiler->compInfoBlkSize;
        gcPtrMapNSize += ptrMapSize;
    }

#endif // DISPLAY_SIZES

    compiler->compInfoBlkSize += ptrMapSize;

    /* Allocate the info block for the method */

    compiler->compInfoBlkAddr = (BYTE*)compiler->info.compCompHnd->allocGCInfo(compiler->compInfoBlkSize);

    /* Fill in the info block and return it to the caller */

    void* infoPtr = compiler->compInfoBlkAddr;

    /* Create the method info block: header followed by GC tracking tables */

    compiler->compInfoBlkAddr +=
        gcInfo.gcInfoBlockHdrSave(compiler->compInfoBlkAddr, -1, codeSize, prologSize, epilogSize, &header, &s_cached);

    assert(compiler->compInfoBlkAddr == (BYTE*)infoPtr + headerSize);
    compiler->compInfoBlkAddr = gcInfo.gcPtrTableSave(compiler->compInfoBlkAddr, header, codeSize, &argTabOffset);
    assert(compiler->compInfoBlkAddr == (BYTE*)infoPtr + headerSize + ptrMapSize);

#ifdef DEBUG

    if (0)
    {
        BYTE*  temp = (BYTE*)infoPtr;
        size_t size = compiler->compInfoBlkAddr - temp;
        BYTE*  ptab = temp + headerSize;

        noway_assert(size == headerSize + ptrMapSize);

        printf("Method info block - header [%zu bytes]:", headerSize);

        for (unsigned i = 0; i < size; i++)
        {
            if (temp == ptab)
            {
                printf("\nMethod info block - ptrtab [%u bytes]:", ptrMapSize);
                printf("\n    %04X: %*c", i & ~0xF, 3 * (i & 0xF), ' ');
            }
            else
            {
                if (!(i % 16))
                    printf("\n    %04X: ", i);
            }

            printf("%02X ", *temp++);
        }

        printf("\n");
    }

#endif // DEBUG

#if DUMP_GC_TABLES

    if (compiler->opts.dspGCtbls)
    {
        const BYTE* base = (BYTE*)infoPtr;
        size_t      size;
        unsigned    methodSize;
        InfoHdr     dumpHeader;

        printf("GC Info for method %s\n", compiler->info.compFullName);
        printf("GC info size = %3u\n", compiler->compInfoBlkSize);

        size = gcInfo.gcInfoBlockHdrDump(base, &dumpHeader, &methodSize);
        // printf("size of header encoding is %3u\n", size);
        printf("\n");

        if (compiler->opts.dspGCtbls)
        {
            base += size;
            size = gcInfo.gcDumpPtrTable(base, dumpHeader, methodSize);
            // printf("size of pointer table is %3u\n", size);
            printf("\n");
            noway_assert(compiler->compInfoBlkAddr == (base + size));
        }
    }

#endif // DUMP_GC_TABLES

    /* Make sure we ended up generating the expected number of bytes */

    noway_assert(compiler->compInfoBlkAddr == (BYTE*)infoPtr + compiler->compInfoBlkSize);

    return infoPtr;
}

#else  // !JIT32_GCENCODER
void CodeGen::genCreateAndStoreGCInfoX64(unsigned codeSize, unsigned prologSize DEBUGARG(void* codePtr))
{
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) CompIAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder);

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

    if (compiler->opts.compDbgEnC)
    {
        // what we have to preserve is called the "frame header" (see comments in VM\eetwain.cpp)
        // which is:
        //  -return address
        //  -saved RBP
        //  -saved bool for synchronized methods

        // slots for ret address + FP + EnC callee-saves
        int preservedAreaSize = (2 + genCountBits((uint64_t)RBM_ENC_CALLEE_SAVED)) * REGSIZE_BYTES;

        if (compiler->info.compFlags & CORINFO_FLG_SYNCH)
        {
            // bool in synchronized methods that tracks whether the lock has been taken (takes a full pointer sized
            // slot)
            preservedAreaSize += TARGET_POINTER_SIZE;

            // Verify that MonAcquired bool is at the bottom of the frame header
            assert(compiler->lvaGetCallerSPRelativeOffset(compiler->lvaMonAcquired) == -preservedAreaSize);
        }

        // Used to signal both that the method is compiled for EnC, and also the size of the block at the top of the
        // frame
        gcInfoEncoder->SetSizeOfEditAndContinuePreservedArea(preservedAreaSize);

        JITDUMP("EnC info:\n");
        JITDUMP("  EnC preserved area size = %d\n", preservedAreaSize);
    }

    if (compiler->opts.IsReversePInvoke())
    {
        unsigned reversePInvokeFrameVarNumber = compiler->lvaReversePInvokeFrameVar;
        assert(reversePInvokeFrameVarNumber != BAD_VAR_NUM);
        const LclVarDsc* reversePInvokeFrameVar = compiler->lvaGetDesc(reversePInvokeFrameVarNumber);
        gcInfoEncoder->SetReversePInvokeFrameSlot(reversePInvokeFrameVar->GetStackOffset());
    }

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
}
#endif // !JIT32_GCENCODER

/*****************************************************************************
 *  Emit a call to a helper function.
 *
 */

void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTargetReg)
{
    void* addr  = nullptr;
    void* pAddr = nullptr;

    emitter::EmitCallType callType = emitter::EC_FUNC_TOKEN;
    addr                           = compiler->compGetHelperFtn((CorInfoHelpFunc)helper, &pAddr);
    regNumber callTarget           = REG_NA;
    regMaskTP killMask             = compiler->compHelperCallKillSet((CorInfoHelpFunc)helper);

    if (!addr)
    {
        assert(pAddr != nullptr);

        // Absolute indirect call addr
        // Note: Order of checks is important. First always check for pc-relative and next
        // zero-relative.  Because the former encoding is 1-byte smaller than the latter.
        if (genCodeIndirAddrCanBeEncodedAsPCRelOffset((size_t)pAddr) ||
            genCodeIndirAddrCanBeEncodedAsZeroRelOffset((size_t)pAddr))
        {
            // generate call whose target is specified by 32-bit offset relative to PC or zero.
            callType = emitter::EC_FUNC_TOKEN_INDIR;
            addr     = pAddr;
        }
        else
        {
#ifdef TARGET_AMD64
            // If this indirect address cannot be encoded as 32-bit offset relative to PC or Zero,
            // load it into REG_HELPER_CALL_TARGET and use register indirect addressing mode to
            // make the call.
            //    mov   reg, addr
            //    call  [reg]

            if (callTargetReg == REG_NA)
            {
                // If a callTargetReg has not been explicitly provided, we will use REG_DEFAULT_HELPER_CALL_TARGET, but
                // this is only a valid assumption if the helper call is known to kill REG_DEFAULT_HELPER_CALL_TARGET.
                callTargetReg            = REG_DEFAULT_HELPER_CALL_TARGET;
                regMaskTP callTargetMask = genRegMask(callTargetReg);
                noway_assert((callTargetMask & killMask) == callTargetMask);
            }
            else
            {
                // The call target must not overwrite any live variable, though it may not be in the
                // kill set for the call.
                regMaskTP callTargetMask = genRegMask(callTargetReg);
                noway_assert((callTargetMask & regSet.GetMaskVars()) == RBM_NONE);
            }
#endif

            callTarget = callTargetReg;
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, callTarget, (ssize_t)pAddr);
            callType = emitter::EC_INDIR_ARD;
        }
    }

    // clang-format off
    GetEmitter()->emitIns_Call(callType,
                               compiler->eeFindHelper(helper),
                               INDEBUG_LDISASM_COMMA(nullptr) addr,
                               argSize,
                               retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(EA_UNKNOWN),
                               gcInfo.gcVarPtrSetCur,
                               gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur,
                               DebugInfo(),
                               callTarget,    // ireg
                               REG_NA, 0, 0,  // xreg, xmul, disp
                               false         // isJump
                               );
    // clang-format on

    regSet.verifyRegistersUsed(killMask);
}

#if defined(DEBUG) && defined(TARGET_AMD64)

/*****************************************************************************
 * Unit tests for the SSE2 instructions.
 */

void CodeGen::genAmd64EmitterUnitTestsSse2()
{
    emitter* theEmitter = GetEmitter();

    //
    // Loads
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

    genDefineTempLabel(genCreateTempLabel());

    // vhaddpd     ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_haddpd, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vaddss      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_addss, EA_4BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vaddsd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_addsd, EA_8BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vaddps      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_addps, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vaddps      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_addps, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vaddpd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_addpd, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vaddpd      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_addpd, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vsubss      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_subss, EA_4BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vsubsd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_subsd, EA_8BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vsubps      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_subps, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vsubps      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_subps, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vsubpd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_subpd, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vsubpd      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_subpd, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vmulss      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_mulss, EA_4BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vmulsd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_mulsd, EA_8BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vmulps      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_mulps, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vmulpd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_mulpd, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vmulps      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_mulps, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vmulpd      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_mulpd, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vandps      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_andps, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vandpd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_andpd, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vandps      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_andps, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vandpd      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_andpd, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vorps      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_orps, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vorpd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_orpd, EA_16BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vorps      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_orps, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vorpd      ymm0,ymm1,ymm2
    GetEmitter()->emitIns_R_R_R(INS_orpd, EA_32BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vdivss      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_divss, EA_4BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vdivsd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_divsd, EA_8BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vdivss      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_divss, EA_4BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vdivsd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_divsd, EA_8BYTE, REG_XMM0, REG_XMM1, REG_XMM2);

    // vdivss      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_cvtss2sd, EA_4BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
    // vdivsd      xmm0,xmm1,xmm2
    GetEmitter()->emitIns_R_R_R(INS_cvtsd2ss, EA_8BYTE, REG_XMM0, REG_XMM1, REG_XMM2);
}

#endif // defined(DEBUG) && defined(TARGET_AMD64)

#ifdef PROFILING_SUPPORTED

#ifdef TARGET_X86

//-----------------------------------------------------------------------------------
// genProfilingEnterCallback: Generate the profiling function enter callback.
//
// Arguments:
//     initReg        - register to use as scratch register
//     pInitRegZeroed - OUT parameter. This variable remains unchanged.
//
// Return Value:
//     None
//
// Notes:
// The x86 profile enter helper has the following requirements (see ProfileEnterNaked in
// VM\i386\asmhelpers.asm for details):
// 1. The calling sequence for calling the helper is:
//          push FunctionIDOrClientID
//          call ProfileEnterHelper
// 2. The calling function has an EBP frame.
// 3. EBP points to the saved ESP which is the first thing saved in the function. Thus,
//    the following prolog is assumed:
//          push ESP
//          mov EBP, ESP
// 4. All registers are preserved.
// 5. The helper pops the FunctionIDOrClientID argument from the stack.
//
void CodeGen::genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    // Give profiler a chance to back out of hooking this method
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    unsigned saveStackLvl2 = genStackLevel;

// Important note: when you change enter probe layout, you must also update SKIP_ENTER_PROF_CALLBACK()
// for x86 stack unwinding

#if defined(UNIX_X86_ABI)
    // Manually align the stack to be 16-byte aligned. This is similar to CodeGen::genAlignStackBeforeCall()
    GetEmitter()->emitIns_R_I(INS_sub, EA_4BYTE, REG_SPBASE, 0xC);
#endif // UNIX_X86_ABI

    // Push the profilerHandle
    if (compiler->compProfilerMethHndIndirected)
    {
        GetEmitter()->emitIns_AR_R(INS_push, EA_PTR_DSP_RELOC, REG_NA, REG_NA, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        inst_IV(INS_push, (size_t)compiler->compProfilerMethHnd);
    }

    // This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov rax, helper addr; call rax"
    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER,
                      0,           // argSize. Again, we have to lie about it
                      EA_UNKNOWN); // retSize

    // Check that we have place for the push.
    assert(compiler->fgGetPtrArgCntMax() >= 1);

#if defined(UNIX_X86_ABI)
    // Restoring alignment manually. This is similar to CodeGen::genRemoveAlignmentAfterCall
    GetEmitter()->emitIns_R_I(INS_add, EA_4BYTE, REG_SPBASE, 0x10);
#endif // UNIX_X86_ABI

    /* Restore the stack level */

    SetStackLevel(saveStackLvl2);
}

//-----------------------------------------------------------------------------------
// genProfilingLeaveCallback: Generate the profiling function leave or tailcall callback.
// Technically, this is not part of the epilog; it is called when we are generating code for a GT_RETURN node.
//
// Arguments:
//     helper - which helper to call. Either CORINFO_HELP_PROF_FCN_LEAVE or CORINFO_HELP_PROF_FCN_TAILCALL
//
// Return Value:
//     None
//
// Notes:
// The x86 profile leave/tailcall helper has the following requirements (see ProfileLeaveNaked and
// ProfileTailcallNaked in VM\i386\asmhelpers.asm for details):
// 1. The calling sequence for calling the helper is:
//          push FunctionIDOrClientID
//          call ProfileLeaveHelper or ProfileTailcallHelper
// 2. The calling function has an EBP frame.
// 3. EBP points to the saved ESP which is the first thing saved in the function. Thus,
//    the following prolog is assumed:
//          push ESP
//          mov EBP, ESP
// 4. helper == CORINFO_HELP_PROF_FCN_LEAVE: All registers are preserved.
//    helper == CORINFO_HELP_PROF_FCN_TAILCALL: Only argument registers are preserved.
// 5. The helper pops the FunctionIDOrClientID argument from the stack.
//
void CodeGen::genProfilingLeaveCallback(unsigned helper)
{
    assert((helper == CORINFO_HELP_PROF_FCN_LEAVE) || (helper == CORINFO_HELP_PROF_FCN_TAILCALL));

    // Only hook if profiler says it's okay.
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    compiler->info.compProfilerCallback = true;

    // Need to save on to the stack level, since the helper call will pop the argument
    unsigned saveStackLvl2 = genStackLevel;

#if defined(UNIX_X86_ABI)
    // Manually align the stack to be 16-byte aligned. This is similar to CodeGen::genAlignStackBeforeCall()
    GetEmitter()->emitIns_R_I(INS_sub, EA_4BYTE, REG_SPBASE, 0xC);
    AddStackLevel(0xC);
    AddNestedAlignment(0xC);
#endif // UNIX_X86_ABI

    //
    // Push the profilerHandle
    //

    if (compiler->compProfilerMethHndIndirected)
    {
        GetEmitter()->emitIns_AR_R(INS_push, EA_PTR_DSP_RELOC, REG_NA, REG_NA, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        inst_IV(INS_push, (size_t)compiler->compProfilerMethHnd);
    }
    genSinglePush();

#if defined(UNIX_X86_ABI)
    int argSize = -REGSIZE_BYTES; // negative means caller-pop (cdecl)
#else
    int argSize = REGSIZE_BYTES;
#endif
    genEmitHelperCall(helper, argSize, EA_UNKNOWN /* retSize */);

    // Check that we have place for the push.
    assert(compiler->fgGetPtrArgCntMax() >= 1);

#if defined(UNIX_X86_ABI)
    // Restoring alignment manually. This is similar to CodeGen::genRemoveAlignmentAfterCall
    GetEmitter()->emitIns_R_I(INS_add, EA_4BYTE, REG_SPBASE, 0x10);
    SubtractStackLevel(0x10);
    SubtractNestedAlignment(0xC);
#endif // UNIX_X86_ABI

    /* Restore the stack level */
    SetStackLevel(saveStackLvl2);
}

#endif // TARGET_X86

#ifdef TARGET_AMD64

//-----------------------------------------------------------------------------------
// genProfilingEnterCallback: Generate the profiling function enter callback.
//
// Arguments:
//     initReg        - register to use as scratch register
//     pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'false' if and only if
//                      this call sets 'initReg' to a non-zero value.
//
// Return Value:
//     None
//
void CodeGen::genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    // Give profiler a chance to back out of hooking this method
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

#if !defined(UNIX_AMD64_ABI)

    unsigned   varNum;
    LclVarDsc* varDsc;

    // Since the method needs to make a profiler callback, it should have out-going arg space allocated.
    noway_assert(compiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
    noway_assert(compiler->lvaOutgoingArgSpaceSize >= (4 * REGSIZE_BYTES));

    // Home all arguments passed in arg registers (RCX, RDX, R8 and R9).
    // In case of vararg methods, arg regs are already homed.
    //
    // Note: Here we don't need to worry about updating gc'info since enter
    // callback is generated as part of prolog which is non-gc interruptible.
    // Moreover GC cannot kick while executing inside profiler callback which is a
    // profiler requirement so it can examine arguments which could be obj refs.
    if (!compiler->info.compIsVarArgs)
    {
        for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->info.compArgsCount; varNum++, varDsc++)
        {
            noway_assert(varDsc->lvIsParam);

            if (!varDsc->lvIsRegArg)
            {
                continue;
            }

            var_types storeType = varDsc->GetRegisterType();
            regNumber argReg    = varDsc->GetArgReg();

            instruction store_ins = ins_Store(storeType);

#ifdef FEATURE_SIMD
            if ((storeType == TYP_SIMD8) && genIsValidIntReg(argReg))
            {
                store_ins = INS_mov;
            }
#endif // FEATURE_SIMD

            GetEmitter()->emitIns_S_R(store_ins, emitTypeSize(storeType), argReg, varNum, 0);
        }
    }

    // Emit profiler EnterCallback(ProfilerMethHnd, caller's SP)
    // RCX = ProfilerMethHnd
    if (compiler->compProfilerMethHndIndirected)
    {
        // Profiler hooks enabled during Ngen time.
        // Profiler handle needs to be accessed through an indirection of a pointer.
        GetEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_8BYTE, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }

    // RDX = caller's SP
    // Notes
    //   1) Here we can query caller's SP offset since prolog will be generated after final frame layout.
    //   2) caller's SP relative offset to FramePointer will be negative.  We need to add absolute value
    //      of that offset to FramePointer to obtain caller's SP value.
    assert(compiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
    int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
    GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_ARG_1, genFramePointerReg(), -callerSPOffset);

    // This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov rax, helper addr; call rax"
    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER, 0, EA_UNKNOWN);

    // TODO-AMD64-CQ: Rather than reloading, see if this could be optimized by combining with prolog
    // generation logic that moves args around as required by first BB entry point conditions
    // computed by LSRA.  Code pointers for investigating this further: genFnPrologCalleeRegArgs()
    // and genEnregisterIncomingStackArgs().
    //
    // Now reload arg registers from home locations.
    // Vararg methods:
    //   - we need to reload only known (i.e. fixed) reg args.
    //   - if floating point type, also reload it into corresponding integer reg
    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->info.compArgsCount; varNum++, varDsc++)
    {
        noway_assert(varDsc->lvIsParam);

        if (!varDsc->lvIsRegArg)
        {
            continue;
        }

        var_types loadType = varDsc->GetRegisterType();
        regNumber argReg   = varDsc->GetArgReg();

        instruction load_ins = ins_Load(loadType);

#ifdef FEATURE_SIMD
        if ((loadType == TYP_SIMD8) && genIsValidIntReg(argReg))
        {
            load_ins = INS_mov;
        }
#endif // FEATURE_SIMD

        GetEmitter()->emitIns_R_S(load_ins, emitTypeSize(loadType), argReg, varNum, 0);

        if (compFeatureVarArg() && compiler->info.compIsVarArgs && varTypeIsFloating(loadType))
        {
            regNumber intArgReg = compiler->getCallArgIntRegister(argReg);
            inst_Mov(TYP_LONG, intArgReg, argReg, /* canSkip */ false, emitActualTypeSize(loadType));
        }
    }

    // If initReg is one of RBM_CALLEE_TRASH, then it needs to be zero'ed before using.
    if ((RBM_CALLEE_TRASH & genRegMask(initReg)) != 0)
    {
        *pInitRegZeroed = false;
    }

#else // !defined(UNIX_AMD64_ABI)

    // Emit profiler EnterCallback(ProfilerMethHnd, caller's SP)
    // R14 = ProfilerMethHnd
    if (compiler->compProfilerMethHndIndirected)
    {
        // Profiler hooks enabled during Ngen time.
        // Profiler handle needs to be accessed through an indirection of a pointer.
        GetEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_PROFILER_ENTER_ARG_0,
                                   (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_PROFILER_ENTER_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }

    // R15 = caller's SP
    // Notes
    //   1) Here we can query caller's SP offset since prolog will be generated after final frame layout.
    //   2) caller's SP relative offset to FramePointer will be negative.  We need to add absolute value
    //      of that offset to FramePointer to obtain caller's SP value.
    assert(compiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
    int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
    GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_PROFILER_ENTER_ARG_1, genFramePointerReg(), -callerSPOffset);

    // We can use any callee trash register (other than RAX, RDI, RSI) for call target.
    // We use R11 here. This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov r11, helper addr; call r11"
    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER, 0, EA_UNKNOWN, REG_DEFAULT_PROFILER_CALL_TARGET);

    // If initReg is one of RBM_CALLEE_TRASH, then it needs to be zero'ed before using.
    if ((RBM_CALLEE_TRASH & genRegMask(initReg)) != 0)
    {
        *pInitRegZeroed = false;
    }

#endif // !defined(UNIX_AMD64_ABI)
}

//-----------------------------------------------------------------------------------
// genProfilingLeaveCallback: Generate the profiling function leave or tailcall callback.
// Technically, this is not part of the epilog; it is called when we are generating code for a GT_RETURN node.
//
// Arguments:
//     helper - which helper to call. Either CORINFO_HELP_PROF_FCN_LEAVE or CORINFO_HELP_PROF_FCN_TAILCALL
//
// Return Value:
//     None
//
void CodeGen::genProfilingLeaveCallback(unsigned helper)
{
    assert((helper == CORINFO_HELP_PROF_FCN_LEAVE) || (helper == CORINFO_HELP_PROF_FCN_TAILCALL));

    // Only hook if profiler says it's okay.
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    compiler->info.compProfilerCallback = true;

#if !defined(UNIX_AMD64_ABI)

    // Since the method needs to make a profiler callback, it should have out-going arg space allocated.
    noway_assert(compiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
    noway_assert(compiler->lvaOutgoingArgSpaceSize >= (4 * REGSIZE_BYTES));

    // If thisPtr needs to be kept alive and reported, it cannot be one of the callee trash
    // registers that profiler callback kills.
    if (compiler->lvaKeepAliveAndReportThis() && compiler->lvaGetDesc(compiler->info.compThisArg)->lvIsInReg())
    {
        regMaskTP thisPtrMask = genRegMask(compiler->lvaGetDesc(compiler->info.compThisArg)->GetRegNum());
        noway_assert((RBM_PROFILER_LEAVE_TRASH & thisPtrMask) == 0);
    }

    // At this point return value is computed and stored in RAX or XMM0.
    // On Amd64, Leave callback preserves the return register.  We keep
    // RAX alive by not reporting as trashed by helper call.  Also note
    // that GC cannot kick-in while executing inside profiler callback,
    // which is a requirement of profiler as well since it needs to examine
    // return value which could be an obj ref.

    // RCX = ProfilerMethHnd
    if (compiler->compProfilerMethHndIndirected)
    {
        // Profiler hooks enabled during Ngen time.
        // Profiler handle needs to be accessed through an indirection of an address.
        GetEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }

    // RDX = caller's SP
    // TODO-AMD64-Cleanup: Once we start doing codegen after final frame layout, retain the "if" portion
    // of the stmnts to execute unconditionally and clean-up rest.
    if (compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT)
    {
        // Caller's SP relative offset to FramePointer will be negative.  We need to add absolute
        // value of that offset to FramePointer to obtain caller's SP value.
        int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_ARG_1, genFramePointerReg(), -callerSPOffset);
    }
    else
    {
        // If we are here means that it is a tentative frame layout during which we
        // cannot use caller's SP offset since it is an estimate.  For now we require the
        // method to have at least a single arg so that we can use it to obtain caller's
        // SP.
        LclVarDsc* varDsc = compiler->lvaGetDesc(0U);
        NYI_IF((varDsc == nullptr) || !varDsc->lvIsParam, "Profiler ELT callback for a method without any params");

        // lea rdx, [FramePointer + Arg0's offset]
        GetEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, REG_ARG_1, 0, 0);
    }

    // We can use any callee trash register (other than RAX, RCX, RDX) for call target.
    // We use R8 here. This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov r8, helper addr; call r8"
    genEmitHelperCall(helper, 0, EA_UNKNOWN, REG_ARG_2);

#else // !defined(UNIX_AMD64_ABI)

    // RDI = ProfilerMethHnd
    if (compiler->compProfilerMethHndIndirected)
    {
        GetEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }

    // RSI = caller's SP
    if (compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT)
    {
        int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_ARG_1, genFramePointerReg(), -callerSPOffset);
    }
    else
    {
        LclVarDsc* varDsc = compiler->lvaGetDesc(0U);
        NYI_IF((varDsc == nullptr) || !varDsc->lvIsParam, "Profiler ELT callback for a method without any params");

        // lea rdx, [FramePointer + Arg0's offset]
        GetEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, REG_ARG_1, 0, 0);
    }

    // We can use any callee trash register (other than RAX, RDI, RSI) for call target.
    // We use R11 here. This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov r11, helper addr; call r11"
    genEmitHelperCall(helper, 0, EA_UNKNOWN, REG_DEFAULT_PROFILER_CALL_TARGET);

#endif // !defined(UNIX_AMD64_ABI)
}

#endif // TARGET_AMD64

#endif // PROFILING_SUPPORTED

#ifdef TARGET_AMD64

//------------------------------------------------------------------------
// genOSRRecordTier0CalleeSavedRegistersAndFrame: for OSR methods, record the
//  subset of callee saves already saved by the Tier0 method, and the frame
//  created by Tier0.
//
void CodeGen::genOSRRecordTier0CalleeSavedRegistersAndFrame()
{
    assert(compiler->compGeneratingProlog);
    assert(compiler->opts.IsOSR());
    assert(compiler->funCurrentFunc()->funKind == FuncKind::FUNC_ROOT);

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

    // Figure out which set of int callee saves was already saved by Tier0.
    // Emit appropriate unwind.
    //
    PatchpointInfo* const patchpointInfo             = compiler->info.compPatchpointInfo;
    regMaskTP const       tier0CalleeSaves           = (regMaskTP)patchpointInfo->CalleeSaveRegisters();
    regMaskTP             tier0IntCalleeSaves        = tier0CalleeSaves & RBM_OSR_INT_CALLEE_SAVED;
    int const             tier0IntCalleeSaveUsedSize = genCountBits(tier0IntCalleeSaves) * REGSIZE_BYTES;

    JITDUMP("--OSR--- tier0 has already saved ");
    JITDUMPEXEC(dspRegMask(tier0IntCalleeSaves));
    JITDUMP("\n");

    // We must account for the Tier0 callee saves.
    //
    // These have already happened at method entry; all these
    // unwind records should be at offset 0.
    //
    // RBP is always aved by Tier0 and always pushed first.
    //
    assert((tier0IntCalleeSaves & RBM_FPBASE) == RBM_FPBASE);
    compiler->unwindPush(REG_RBP);
    tier0IntCalleeSaves &= ~RBM_FPBASE;

    // Now the rest of the Tier0 callee saves.
    //
    for (regNumber reg = REG_INT_LAST; tier0IntCalleeSaves != RBM_NONE; reg = REG_PREV(reg))
    {
        regMaskTP regBit = genRegMask(reg);

        if ((regBit & tier0IntCalleeSaves) != 0)
        {
            compiler->unwindPush(reg);
        }
        tier0IntCalleeSaves &= ~regBit;
    }

    // We must account for the post-callee-saves push SP movement
    // done by the Tier0 frame and by the OSR transition.
    //
    // tier0FrameSize is the Tier0 FP-SP delta plus the fake call slot added by
    // JIT_Patchpoint. We add one slot to account for the saved FP.
    //
    // We then need to subtract off the size the Tier0 callee saves as SP
    // adjusts for those will have been modelled by the unwind pushes above.
    //
    int const tier0FrameSize = patchpointInfo->TotalFrameSize() + REGSIZE_BYTES;
    int const tier0NetSize   = tier0FrameSize - tier0IntCalleeSaveUsedSize;
    compiler->unwindAllocStack(tier0NetSize);
}

//------------------------------------------------------------------------
// genOSRSaveRemainingCalleeSavedRegisters: save any callee save registers
//   that Tier0 didn't save.
//
// Notes:
//   This must be invoked after SP has been adjusted to allocate the local
//   frame, because of how the UnwindSave records are interpreted.
//
//   We rely on the fact that other "local frame" allocation actions (like
//    stack probing) will not trash callee saves registers.
//
void CodeGen::genOSRSaveRemainingCalleeSavedRegisters()
{
    // We should be generating the prolog of an OSR root frame.
    //
    assert(compiler->compGeneratingProlog);
    assert(compiler->opts.IsOSR());
    assert(compiler->funCurrentFunc()->funKind == FuncKind::FUNC_ROOT);

    // x86/x64 doesn't support push of xmm/ymm regs, therefore consider only integer registers for pushing onto stack
    // here. Space for float registers to be preserved is stack allocated and saved as part of prolog sequence and not
    // here.
    regMaskTP rsPushRegs = regSet.rsGetModifiedRegsMask() & RBM_OSR_INT_CALLEE_SAVED;

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

    // Figure out which set of int callee saves still needs saving.
    //
    PatchpointInfo* const patchpointInfo              = compiler->info.compPatchpointInfo;
    regMaskTP const       tier0CalleeSaves            = (regMaskTP)patchpointInfo->CalleeSaveRegisters();
    regMaskTP             tier0IntCalleeSaves         = tier0CalleeSaves & RBM_OSR_INT_CALLEE_SAVED;
    unsigned const        tier0IntCalleeSaveUsedSize  = genCountBits(tier0IntCalleeSaves) * REGSIZE_BYTES;
    regMaskTP const       osrIntCalleeSaves           = rsPushRegs & RBM_OSR_INT_CALLEE_SAVED;
    regMaskTP             osrAdditionalIntCalleeSaves = osrIntCalleeSaves & ~tier0IntCalleeSaves;

    JITDUMP("---OSR--- int callee saves are ");
    JITDUMPEXEC(dspRegMask(osrIntCalleeSaves));
    JITDUMP("; tier0 already saved ");
    JITDUMPEXEC(dspRegMask(tier0IntCalleeSaves));
    JITDUMP("; so only saving ");
    JITDUMPEXEC(dspRegMask(osrAdditionalIntCalleeSaves));
    JITDUMP("\n");

    // These remaining callee saves will be stored in the Tier0 callee save area
    // below any saves already done by Tier0. Compute the offset.
    //
    // The OSR method doesn't actually use its callee save area.
    //
    int const osrFrameSize        = compiler->compLclFrameSize;
    int const tier0FrameSize      = patchpointInfo->TotalFrameSize();
    int const osrCalleeSaveSize   = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
    int const osrFramePointerSize = isFramePointerUsed() ? REGSIZE_BYTES : 0;
    int offset = osrFrameSize + osrCalleeSaveSize + osrFramePointerSize + tier0FrameSize - tier0IntCalleeSaveUsedSize;

    // The tier0 frame is always an RBP frame, so the OSR method should never need to save RBP.
    //
    assert((tier0CalleeSaves & RBM_FPBASE) == RBM_FPBASE);
    assert((osrAdditionalIntCalleeSaves & RBM_FPBASE) == RBM_NONE);

    // The OSR method must use MOVs to save additional callee saves.
    //
    for (regNumber reg = REG_INT_LAST; osrAdditionalIntCalleeSaves != RBM_NONE; reg = REG_PREV(reg))
    {
        regMaskTP regBit = genRegMask(reg);

        if ((regBit & osrAdditionalIntCalleeSaves) != 0)
        {
            GetEmitter()->emitIns_AR_R(INS_mov, EA_8BYTE, reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
            offset -= REGSIZE_BYTES;
        }
        osrAdditionalIntCalleeSaves &= ~regBit;
    }
}

#endif // TARGET_AMD64

//------------------------------------------------------------------------
// genPushCalleeSavedRegisters: Push any callee-saved registers we have used.
//
void CodeGen::genPushCalleeSavedRegisters()
{
    assert(compiler->compGeneratingProlog);

#if DEBUG
    // OSR root frames must handle this differently. See
    //   genOSRRecordTier0CalleeSavedRegisters()
    //   genOSRSaveRemainingCalleeSavedRegisters()
    //
    if (compiler->opts.IsOSR())
    {
        assert(compiler->funCurrentFunc()->funKind != FuncKind::FUNC_ROOT);
    }
#endif

    // x86/x64 doesn't support push of xmm/ymm regs, therefore consider only integer registers for pushing onto stack
    // here. Space for float registers to be preserved is stack allocated and saved as part of prolog sequence and not
    // here.
    regMaskTP rsPushRegs = regSet.rsGetModifiedRegsMask() & RBM_INT_CALLEE_SAVED;

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

    // On X86/X64 we have already pushed the FP (frame-pointer) prior to calling this method
    if (isFramePointerUsed())
    {
        rsPushRegs &= ~RBM_FPBASE;
    }

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

    // Push backwards so we match the order we will pop them in the epilog
    // and all the other code that expects it to be in this order.
    for (regNumber reg = REG_INT_LAST; rsPushRegs != RBM_NONE; reg = REG_PREV(reg))
    {
        regMaskTP regBit = genRegMask(reg);

        if ((regBit & rsPushRegs) != 0)
        {
            inst_RV(INS_push, reg, TYP_REF);
            compiler->unwindPush(reg);
            rsPushRegs &= ~regBit;
        }
    }
}

void CodeGen::genPopCalleeSavedRegisters(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

#ifdef TARGET_AMD64

    const bool isFunclet                = compiler->funCurrentFunc()->funKind != FuncKind::FUNC_ROOT;
    const bool doesSupersetOfNormalPops = compiler->opts.IsOSR() && !isFunclet;

    // OSR methods must restore all registers saved by either the OSR or
    // the Tier0 method. First restore any callee save not saved by
    // Tier0, then the callee saves done by Tier0.
    //
    // OSR funclets do normal restores.
    //
    if (doesSupersetOfNormalPops)
    {
        regMaskTP rsPopRegs = regSet.rsGetModifiedRegsMask() & RBM_OSR_INT_CALLEE_SAVED;
        regMaskTP tier0CalleeSaves =
            ((regMaskTP)compiler->info.compPatchpointInfo->CalleeSaveRegisters()) & RBM_OSR_INT_CALLEE_SAVED;
        regMaskTP additionalCalleeSaves = rsPopRegs & ~tier0CalleeSaves;

        // Registers saved by the OSR prolog.
        //
        genPopCalleeSavedRegistersFromMask(additionalCalleeSaves);

        // Registers saved by the Tier0 prolog.
        // Tier0 frame pointer will be restored separately.
        //
        genPopCalleeSavedRegistersFromMask(tier0CalleeSaves & ~RBM_FPBASE);
        return;
    }

#endif // TARGET_AMD64

    // Registers saved by a normal prolog
    //
    regMaskTP      rsPopRegs = regSet.rsGetModifiedRegsMask() & RBM_INT_CALLEE_SAVED;
    const unsigned popCount  = genPopCalleeSavedRegistersFromMask(rsPopRegs);
    noway_assert(compiler->compCalleeRegsPushed == popCount);
}

//------------------------------------------------------------------------
// genPopCalleeSavedRegistersFromMask: pop specified set of callee saves
//   in the "standard" order
//
unsigned CodeGen::genPopCalleeSavedRegistersFromMask(regMaskTP rsPopRegs)
{
    unsigned popCount = 0;
    if ((rsPopRegs & RBM_EBX) != 0)
    {
        popCount++;
        inst_RV(INS_pop, REG_EBX, TYP_I_IMPL);
    }
    if ((rsPopRegs & RBM_FPBASE) != 0)
    {
        // EBP cannot be directly modified for EBP frame and double-aligned frames
        assert(!doubleAlignOrFramePointerUsed());

        popCount++;
        inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
    }

#ifndef UNIX_AMD64_ABI
    // For System V AMD64 calling convention ESI and EDI are volatile registers.
    if ((rsPopRegs & RBM_ESI) != 0)
    {
        popCount++;
        inst_RV(INS_pop, REG_ESI, TYP_I_IMPL);
    }
    if ((rsPopRegs & RBM_EDI) != 0)
    {
        popCount++;
        inst_RV(INS_pop, REG_EDI, TYP_I_IMPL);
    }
#endif // !defined(UNIX_AMD64_ABI)

#ifdef TARGET_AMD64
    if ((rsPopRegs & RBM_R12) != 0)
    {
        popCount++;
        inst_RV(INS_pop, REG_R12, TYP_I_IMPL);
    }
    if ((rsPopRegs & RBM_R13) != 0)
    {
        popCount++;
        inst_RV(INS_pop, REG_R13, TYP_I_IMPL);
    }
    if ((rsPopRegs & RBM_R14) != 0)
    {
        popCount++;
        inst_RV(INS_pop, REG_R14, TYP_I_IMPL);
    }
    if ((rsPopRegs & RBM_R15) != 0)
    {
        popCount++;
        inst_RV(INS_pop, REG_R15, TYP_I_IMPL);
    }
#endif // TARGET_AMD64

    // Amd64/x86 doesn't support push/pop of xmm registers.
    // These will get saved to stack separately after allocating
    // space on stack in prolog sequence.  PopCount is essentially
    // tracking the count of integer registers pushed.

    return popCount;
}

/*****************************************************************************
 *
 *  Generates code for a function epilog.
 *
 *  Please consult the "debugger team notification" comment in genFnProlog().
 */

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, GetEmitter()->emitInitGCrefVars);
    gcInfo.gcRegGCrefSetCur = GetEmitter()->emitInitGCrefRegs;
    gcInfo.gcRegByrefSetCur = GetEmitter()->emitInitByrefRegs;

    noway_assert(!compiler->opts.MinOpts() || isFramePointerUsed()); // FPO not allowed with minOpts

#ifdef DEBUG
    genInterruptibleUsed = true;
#endif

    bool jmpEpilog = block->HasFlag(BBF_HAS_JMP);

#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n__epilog:\n");
    }

    if (verbose)
    {
        printf("gcVarPtrSetCur=%s ", VarSetOps::ToString(compiler, gcInfo.gcVarPtrSetCur));
        dumpConvertedVarSet(compiler, gcInfo.gcVarPtrSetCur);
        printf(", gcRegGCrefSetCur=");
        printRegMaskInt(gcInfo.gcRegGCrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur);
        printf(", gcRegByrefSetCur=");
        printRegMaskInt(gcInfo.gcRegByrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur);
        printf("\n");
    }
#endif

    // Restore float registers that were saved to stack before SP is modified.
    genRestoreCalleeSavedFltRegs(compiler->compLclFrameSize);

#ifdef JIT32_GCENCODER
    // When using the JIT32 GC encoder, we do not start the OS-reported portion of the epilog until after
    // the above call to `genRestoreCalleeSavedFltRegs` because that function
    //   a) does not actually restore any registers: there are none when targeting the Windows x86 ABI,
    //      which is the only target that uses the JIT32 GC encoder
    //   b) may issue a `vzeroupper` instruction to eliminate AVX -> SSE transition penalties.
    // Because the `vzeroupper` instruction is not recognized by the VM's unwinder and there are no
    // callee-save FP restores that the unwinder would need to see, we can avoid the need to change the
    // unwinder (and break binary compat with older versions of the runtime) by starting the epilog
    // after any `vzeroupper` instruction has been emitted. If either of the above conditions changes,
    // we will need to rethink this.
    GetEmitter()->emitStartEpilog();
#endif

    /* Compute the size in bytes we've pushed/popped */

    bool removeEbpFrame = doubleAlignOrFramePointerUsed();

#ifdef TARGET_AMD64
    // We only remove the EBP frame using the frame pointer (using `lea rsp, [rbp + const]`)
    // if we reported the frame pointer in the prolog. The Windows x64 unwinding ABI specifically
    // disallows this `lea` form:
    //
    //    See https://docs.microsoft.com/en-us/cpp/build/prolog-and-epilog?view=msvc-160#epilog-code
    //
    //    "When a frame pointer is not used, the epilog must use add RSP,constant to deallocate the fixed part of the
    //    stack. It may not use lea RSP,constant[RSP] instead. This restriction exists so the unwind code has fewer
    //    patterns to recognize when searching for epilogs."
    //
    // Otherwise, we must use `add RSP, constant`, as stated. So, we need to use the same condition
    // as genFnProlog() used in determining whether to report the frame pointer in the unwind data.
    // This is a subset of the `doubleAlignOrFramePointerUsed()` cases.
    //
    if (removeEbpFrame)
    {
        const bool reportUnwindData = compiler->compLocallocUsed || compiler->opts.compDbgEnC;
        removeEbpFrame              = removeEbpFrame && reportUnwindData;
    }
#endif // TARGET_AMD64

    if (!removeEbpFrame)
    {
        // We have an ESP frame */

        noway_assert(compiler->compLocallocUsed == false); // Only used with frame-pointer
        /* Get rid of our local variables */
        unsigned int frameSize = compiler->compLclFrameSize;

#ifdef TARGET_AMD64

        // OSR must remove the entire OSR frame and the Tier0 frame down to the bottom
        // of the used part of the Tier0 callee save area.
        //
        if (compiler->opts.IsOSR())
        {
            // The patchpoint TotalFrameSize is SP-FP delta (plus "call" slot added by JIT_Patchpoint)
            // so does not account for the Tier0 push of FP, so we add in an extra stack slot to get the
            // offset to the top of the Tier0 callee saves area.
            //
            PatchpointInfo* const patchpointInfo = compiler->info.compPatchpointInfo;

            regMaskTP const tier0CalleeSaves           = (regMaskTP)patchpointInfo->CalleeSaveRegisters();
            regMaskTP const tier0IntCalleeSaves        = tier0CalleeSaves & RBM_OSR_INT_CALLEE_SAVED;
            regMaskTP const osrIntCalleeSaves          = regSet.rsGetModifiedRegsMask() & RBM_OSR_INT_CALLEE_SAVED;
            regMaskTP const allIntCalleeSaves          = osrIntCalleeSaves | tier0IntCalleeSaves;
            unsigned const  tier0FrameSize             = patchpointInfo->TotalFrameSize() + REGSIZE_BYTES;
            unsigned const  tier0IntCalleeSaveUsedSize = genCountBits(allIntCalleeSaves) * REGSIZE_BYTES;
            unsigned const  osrCalleeSaveSize          = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
            unsigned const  osrFramePointerSize        = isFramePointerUsed() ? REGSIZE_BYTES : 0;
            unsigned const  osrAdjust =
                tier0FrameSize - tier0IntCalleeSaveUsedSize + osrCalleeSaveSize + osrFramePointerSize;

            JITDUMP("OSR epilog adjust factors: tier0 frame %u, tier0 callee saves -%u, osr callee saves %u, osr "
                    "framePointer %u\n",
                    tier0FrameSize, tier0IntCalleeSaveUsedSize, osrCalleeSaveSize, osrFramePointerSize);
            JITDUMP("    OSR frame size %u; net osr adjust %u, result %u\n", frameSize, osrAdjust,
                    frameSize + osrAdjust);
            frameSize += osrAdjust;
        }
#endif // TARGET_AMD64

        if (frameSize > 0)
        {
#ifdef TARGET_X86
            /* Add 'compiler->compLclFrameSize' to ESP */
            /* Use pop ECX to increment ESP by 4, unless compiler->compJmpOpUsed is true */

            if ((frameSize == TARGET_POINTER_SIZE) && !compiler->compJmpOpUsed)
            {
                inst_RV(INS_pop, REG_ECX, TYP_I_IMPL);
                regSet.verifyRegUsed(REG_ECX);
            }
            else
#endif // TARGET_X86
            {
                /* Add 'compiler->compLclFrameSize' to ESP */
                /* Generate "add esp, <stack-size>" */
                inst_RV_IV(INS_add, REG_SPBASE, frameSize, EA_PTRSIZE);
            }
        }

        genPopCalleeSavedRegisters();

#ifdef TARGET_AMD64
        // In the case where we have an RSP frame, and no frame pointer reported in the OS unwind info,
        // but we do have a pushed frame pointer and established frame chain, we do need to pop RBP.
        //
        // OSR methods must always pop RBP (pushed by Tier0 frame)
        if (doubleAlignOrFramePointerUsed() || compiler->opts.IsOSR())
        {
            inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
        }
#endif // TARGET_AMD64
    }
    else
    {
        noway_assert(doubleAlignOrFramePointerUsed());

        // We don't support OSR for methods that must report an FP in unwind.
        //
        assert(!compiler->opts.IsOSR());

        /* Tear down the stack frame */

        bool needMovEspEbp = false;

#if DOUBLE_ALIGN
        if (compiler->genDoubleAlign())
        {
            //
            // add esp, compLclFrameSize
            //
            // We need not do anything (except the "mov esp, ebp") if
            // compiler->compCalleeRegsPushed==0. However, this is unlikely, and it
            // also complicates the code manager. Hence, we ignore that case.

            noway_assert(compiler->compLclFrameSize != 0);
            inst_RV_IV(INS_add, REG_SPBASE, compiler->compLclFrameSize, EA_PTRSIZE);

            needMovEspEbp = true;
        }
        else
#endif // DOUBLE_ALIGN
        {
            bool needLea = false;

            if (compiler->compLocallocUsed)
            {
                // OSR not yet ready for localloc
                assert(!compiler->opts.IsOSR());

                // ESP may be variable if a localloc was actually executed. Reset it.
                //    lea esp, [ebp - compiler->compCalleeRegsPushed * REGSIZE_BYTES]
                needLea = true;
            }
            else if (!regSet.rsRegsModified(RBM_CALLEE_SAVED))
            {
                if (compiler->compLclFrameSize != 0)
                {
#ifdef TARGET_AMD64
                    // AMD64 can't use "mov esp, ebp", according to the ABI specification describing epilogs. So,
                    // do an LEA to "pop off" the frame allocation.
                    needLea = true;
#else  // !TARGET_AMD64
                    // We will just generate "mov esp, ebp" and be done with it.
                    needMovEspEbp = true;
#endif // !TARGET_AMD64
                }
            }
            else if (compiler->compLclFrameSize == 0)
            {
                // do nothing before popping the callee-saved registers
            }
#ifdef TARGET_X86
            else if (compiler->compLclFrameSize == REGSIZE_BYTES)
            {
                // "pop ecx" will make ESP point to the callee-saved registers
                inst_RV(INS_pop, REG_ECX, TYP_I_IMPL);
                regSet.verifyRegUsed(REG_ECX);
            }
#endif // TARGET_X86
            else
            {
                // We need to make ESP point to the callee-saved registers
                needLea = true;
            }

            if (needLea)
            {
                int offset;

#ifdef TARGET_AMD64
                // lea esp, [ebp + compiler->compLclFrameSize - genSPtoFPdelta]
                //
                // Case 1: localloc not used.
                // genSPToFPDelta = compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize
                // offset = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
                // The amount to be subtracted from RBP to point at callee saved int regs.
                //
                // Case 2: localloc used
                // genSPToFPDelta = Min(240, (int)compiler->lvaOutgoingArgSpaceSize)
                // Offset = Amount to be added to RBP to point at callee saved int regs.
                offset = genSPtoFPdelta() - compiler->compLclFrameSize;

                // Offset should fit within a byte if localloc is not used.
                if (!compiler->compLocallocUsed)
                {
                    noway_assert(offset < UCHAR_MAX);
                }
#else
                // lea esp, [ebp - compiler->compCalleeRegsPushed * REGSIZE_BYTES]
                offset = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
                noway_assert(offset < UCHAR_MAX); // the offset fits in a byte
#endif

                GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, -offset);
            }
        }

        //
        // Pop the callee-saved registers (if any)
        //
        genPopCalleeSavedRegisters();

#ifdef TARGET_AMD64
        // Extra OSR adjust to get to where RBP was saved by the tier0 frame.
        //
        // Note the other callee saves made in that frame are dead, the current method
        // will save and restore what it needs.
        if (compiler->opts.IsOSR())
        {
            PatchpointInfo* const patchpointInfo = compiler->info.compPatchpointInfo;
            const int             tier0FrameSize = patchpointInfo->TotalFrameSize();

            // Use add since we know the SP-to-FP delta of the original method.
            // We also need to skip over the slot where we pushed RBP.
            //
            // If we ever allow the original method to have localloc this will
            // need to change.
            inst_RV_IV(INS_add, REG_SPBASE, tier0FrameSize + TARGET_POINTER_SIZE, EA_PTRSIZE);
        }

        assert(!needMovEspEbp); // "mov esp, ebp" is not allowed in AMD64 epilogs
#else                           // !TARGET_AMD64
        if (needMovEspEbp)
        {
            // mov esp, ebp
            inst_Mov(TYP_I_IMPL, REG_SPBASE, REG_FPBASE, /* canSkip */ false);
        }
#endif                          // !TARGET_AMD64

        // pop ebp
        inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
    }

    GetEmitter()->emitStartExitSeq(); // Mark the start of the "return" sequence

    /* Check if this a special return block i.e.
     * CEE_JMP instruction */

    if (jmpEpilog)
    {
        noway_assert(block->KindIs(BBJ_RETURN));
        noway_assert(block->GetFirstLIRNode());

        // figure out what jump we have
        GenTree* jmpNode = block->lastNode();
#if !FEATURE_FASTTAILCALL
        // x86
        noway_assert(jmpNode->gtOper == GT_JMP);
#else
        // amd64
        // If jmpNode is GT_JMP then gtNext must be null.
        // If jmpNode is a fast tail call, gtNext need not be null since it could have embedded stmts.
        noway_assert((jmpNode->gtOper != GT_JMP) || (jmpNode->gtNext == nullptr));

        // Could either be a "jmp method" or "fast tail call" implemented as epilog+jmp
        noway_assert((jmpNode->gtOper == GT_JMP) ||
                     ((jmpNode->gtOper == GT_CALL) && jmpNode->AsCall()->IsFastTailCall()));

        // The next block is associated with this "if" stmt
        if (jmpNode->gtOper == GT_JMP)
#endif
        {
            // Simply emit a jump to the methodHnd. This is similar to a call so we can use
            // the same descriptor with some minor adjustments.
            CORINFO_METHOD_HANDLE methHnd = (CORINFO_METHOD_HANDLE)jmpNode->AsVal()->gtVal1;

            CORINFO_CONST_LOOKUP addrInfo;
            compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo);
            if (addrInfo.accessType != IAT_VALUE && addrInfo.accessType != IAT_PVALUE)
            {
                NO_WAY("Unsupported JMP indirection");
            }

            // If we have IAT_PVALUE we might need to jump via register indirect, as sometimes the
            // indirection cell can't be reached by the jump.
            emitter::EmitCallType callType;
            void*                 addr;
            regNumber             indCallReg;

            if (addrInfo.accessType == IAT_PVALUE)
            {
                if (genCodeIndirAddrCanBeEncodedAsPCRelOffset((size_t)addrInfo.addr))
                {
                    // 32 bit displacement will work
                    callType   = emitter::EC_FUNC_TOKEN_INDIR;
                    addr       = addrInfo.addr;
                    indCallReg = REG_NA;
                }
                else
                {
                    // 32 bit displacement won't work
                    callType   = emitter::EC_INDIR_ARD;
                    indCallReg = REG_RAX;
                    addr       = nullptr;
                    instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addrInfo.addr);
                    regSet.verifyRegUsed(indCallReg);
                }
            }
            else
            {
                callType   = emitter::EC_FUNC_TOKEN;
                addr       = addrInfo.addr;
                indCallReg = REG_NA;
            }

            // clang-format off
            GetEmitter()->emitIns_Call(callType,
                                       methHnd,
                                       INDEBUG_LDISASM_COMMA(nullptr)
                                       addr,
                                       0,                                                      // argSize
                                       EA_UNKNOWN                                              // retSize
                                       MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(EA_UNKNOWN),        // secondRetSize
                                       gcInfo.gcVarPtrSetCur,
                                       gcInfo.gcRegGCrefSetCur,
                                       gcInfo.gcRegByrefSetCur,
                                       DebugInfo(),
                                       indCallReg, REG_NA, 0, 0,  /* ireg, xreg, xmul, disp */
                                       true /* isJump */
            );
            // clang-format on
        }
#if FEATURE_FASTTAILCALL
        else
        {
            genCallInstruction(jmpNode->AsCall());
        }
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        unsigned stkArgSize = 0; // Zero on all platforms except x86

#if defined(TARGET_X86)
        bool fCalleePop = true;

        // varargs has caller pop
        if (compiler->info.compIsVarArgs)
            fCalleePop = false;

        if (IsCallerPop(compiler->info.compCallConv))
            fCalleePop = false;

        if (fCalleePop)
        {
            noway_assert(compiler->compArgSize >= intRegState.rsCalleeRegArgCount * REGSIZE_BYTES);
            stkArgSize = compiler->compArgSize - intRegState.rsCalleeRegArgCount * REGSIZE_BYTES;

            noway_assert(compiler->compArgSize < 0x10000); // "ret" only has 2 byte operand
        }

#ifdef UNIX_X86_ABI
        // The called function must remove hidden address argument from the stack before returning
        // in case of struct returning according to cdecl calling convention on linux.
        // Details: http://www.sco.com/developers/devspecs/abi386-4.pdf pages 40-43
        if (compiler->info.compCallConv == CorInfoCallConvExtension::C && compiler->info.compRetBuffArg != BAD_VAR_NUM)
            stkArgSize += TARGET_POINTER_SIZE;
#endif // UNIX_X86_ABI
#endif // TARGET_X86

        /* Return, popping our arguments (if any) */
        instGen_Return(stkArgSize);
    }
}

#if defined(FEATURE_EH_FUNCLETS)

#if defined(TARGET_AMD64)

/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 *
 *  Funclets have the following incoming arguments:
 *
 *      catch/filter-handler: rcx = InitialSP, rdx = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:               rcx = InitialSP, rdx = the exception object to filter (see GT_CATCH_ARG)
 *      finally/fault:        rcx = InitialSP
 *
 *  Funclets set the following registers on exit:
 *
 *      catch/filter-handler: rax = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:               rax = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:        none
 *
 *  The AMD64 funclet prolog sequence is:
 *
 *     push ebp
 *     push callee-saved regs
 *                      ; TODO-AMD64-CQ: We probably only need to save any callee-save registers that we actually use
 *                      ;         in the funclet. Currently, we save the same set of callee-saved regs calculated for
 *                      ;         the entire function.
 *     sub sp, XXX      ; Establish the rest of the frame.
 *                      ;   XXX is determined by lvaOutgoingArgSpaceSize plus space for the PSP slot, aligned
 *                      ;   up to preserve stack alignment. If we push an odd number of registers, we also
 *                      ;   generate this, to keep the stack aligned.
 *
 *     ; Fill the PSP slot, for use by the VM (it gets reported with the GC info), or by code generation of nested
 *     ;    filters.
 *     ; This is not part of the "OS prolog"; it has no associated unwind data, and is not reversed in the funclet
 *     ;    epilog.
 *     ; Also, re-establish the frame pointer from the PSP.
 *
 *     mov rbp, [rcx + PSP_slot_InitialSP_offset]       ; Load the PSP (InitialSP of the main function stored in the
 *                                                      ; PSP of the dynamically containing funclet or function)
 *     mov [rsp + PSP_slot_InitialSP_offset], rbp       ; store the PSP in our frame
 *     lea ebp, [rbp + Function_InitialSP_to_FP_delta]  ; re-establish the frame pointer of the parent frame. If
 *                                                      ; Function_InitialSP_to_FP_delta==0, we don't need this
 *                                                      ; instruction.
 *
 *  The epilog sequence is then:
 *
 *     add rsp, XXX
 *     pop callee-saved regs    ; if necessary
 *     pop rbp
 *     ret
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |    Return address     |
 *      |-----------------------|
 *      |      Saved EBP        |
 *      |-----------------------|
 *      |Callee saved registers |
 *      |-----------------------|
 *      ~  possible 8 byte pad  ~
 *      ~     for alignment     ~
 *      |-----------------------|
 *      |        PSP slot       | // Omitted in NativeAOT ABI
 *      |-----------------------|
 *      |   Outgoing arg space  | // this only exists if the function makes a call
 *      |-----------------------| <---- Initial SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 * TODO-AMD64-Bug?: the frame pointer should really point to the PSP slot (the debugger seems to assume this
 * in DacDbiInterfaceImpl::InitParentFrameInfo()), or someplace above Initial-SP. There is an AMD64
 * UNWIND_INFO restriction that it must be within 240 bytes of Initial-SP. See jit64\amd64\inc\md.h
 * "FRAMEPTR OFFSETS" for details.
 */

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif

    assert(!regSet.rsRegsModified(RBM_FPBASE));
    assert(block != nullptr);
    assert(block->HasFlag(BBF_FUNCLET_BEG));
    assert(isFramePointerUsed());

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    // We need to push ebp, since it's callee-saved.
    // We need to push the callee-saved registers. We only need to push the ones that we need, but we don't
    // keep track of that on a per-funclet basis, so we push the same set as in the main function.
    // The only fixed-size frame we need to allocate is whatever is big enough for the PSPSym, since nothing else
    // is stored here (all temps are allocated in the parent frame).
    // We do need to allocate the outgoing argument space, in case there are calls here. This must be the same
    // size as the parent frame's outgoing argument space, to keep the PSPSym offset the same.

    inst_RV(INS_push, REG_FPBASE, TYP_REF);
    compiler->unwindPush(REG_FPBASE);

    // Callee saved int registers are pushed to stack.
    genPushCalleeSavedRegisters();

    regMaskTP maskArgRegsLiveIn;
    if ((block->bbCatchTyp == BBCT_FINALLY) || (block->bbCatchTyp == BBCT_FAULT))
    {
        maskArgRegsLiveIn = RBM_ARG_0;
    }
    else
    {
        maskArgRegsLiveIn = RBM_ARG_0 | RBM_ARG_2;
    }

    regNumber initReg       = REG_EBP; // We already saved EBP, so it can be trashed
    bool      initRegZeroed = false;

    genAllocLclFrame(genFuncletInfo.fiSpDelta, initReg, &initRegZeroed, maskArgRegsLiveIn);

    // Callee saved float registers are copied to stack in their assigned stack slots
    // after allocating space for them as part of funclet frame.
    genPreserveCalleeSavedFltRegs(genFuncletInfo.fiSpDelta);

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // If there is no PSPSym (NativeAOT ABI), we are done.
    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    GetEmitter()->emitIns_R_AR(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_ARG_0, genFuncletInfo.fiPSP_slot_InitialSP_offset);

    regSet.verifyRegUsed(REG_FPBASE);

    GetEmitter()->emitIns_AR_R(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, genFuncletInfo.fiPSP_slot_InitialSP_offset);

    if (genFuncletInfo.fiFunction_InitialSP_to_FP_delta != 0)
    {
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_FPBASE, REG_FPBASE,
                                   genFuncletInfo.fiFunction_InitialSP_to_FP_delta);
    }

    // We've modified EBP, but not really. Say that we haven't...
    regSet.rsRemoveRegsModified(RBM_FPBASE);
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 *
 *  Note that we don't do anything with unwind codes, because AMD64 only cares about unwind codes for the prolog.
 */

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    // Restore callee saved XMM regs from their stack slots before modifying SP
    // to position at callee saved int regs.
    genRestoreCalleeSavedFltRegs(genFuncletInfo.fiSpDelta);
    inst_RV_IV(INS_add, REG_SPBASE, genFuncletInfo.fiSpDelta, EA_PTRSIZE);
    genPopCalleeSavedRegisters();
    inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
    instGen_Return(0);
}

/*****************************************************************************
 *
 *  Capture the information used to generate the funclet prologs and epilogs.
 */

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    if (!compiler->ehAnyFunclets())
    {
        return;
    }

    // Note that compLclFrameSize can't be used (for can we call functions that depend on it),
    // because we're not going to allocate the same size frame as the parent.

    assert(isFramePointerUsed());
    assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT); // The frame size and offsets must be
                                                                          // finalized
    assert(compiler->compCalleeFPRegsSavedMask != (regMaskTP)-1); // The float registers to be preserved is finalized

    // Even though lvaToInitialSPRelativeOffset() depends on compLclFrameSize,
    // that's ok, because we're figuring out an offset in the parent frame.
    genFuncletInfo.fiFunction_InitialSP_to_FP_delta =
        compiler->lvaToInitialSPRelativeOffset(0, true); // trick to find the Initial-SP-relative offset of the frame
                                                         // pointer.

    assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
#ifndef UNIX_AMD64_ABI
    // No 4 slots for outgoing params on the stack for System V systems.
    assert((compiler->lvaOutgoingArgSpaceSize == 0) ||
           (compiler->lvaOutgoingArgSpaceSize >= (4 * REGSIZE_BYTES))); // On AMD64, we always have 4 outgoing argument
// slots if there are any calls in the function.
#endif // UNIX_AMD64_ABI
    unsigned offset = compiler->lvaOutgoingArgSpaceSize;

    genFuncletInfo.fiPSP_slot_InitialSP_offset = offset;

    // How much stack do we allocate in the funclet?
    // We need to 16-byte align the stack.

    unsigned totalFrameSize =
        REGSIZE_BYTES                                       // return address
        + REGSIZE_BYTES                                     // pushed EBP
        + (compiler->compCalleeRegsPushed * REGSIZE_BYTES); // pushed callee-saved int regs, not including EBP

    // Entire 128-bits of XMM register is saved to stack due to ABI encoding requirement.
    // Copying entire XMM register to/from memory will be performant if SP is aligned at XMM_REGSIZE_BYTES boundary.
    unsigned calleeFPRegsSavedSize = genCountBits(compiler->compCalleeFPRegsSavedMask) * XMM_REGSIZE_BYTES;
    unsigned FPRegsPad             = (calleeFPRegsSavedSize > 0) ? AlignmentPad(totalFrameSize, XMM_REGSIZE_BYTES) : 0;

    unsigned PSPSymSize = (compiler->lvaPSPSym != BAD_VAR_NUM) ? REGSIZE_BYTES : 0;

    totalFrameSize += FPRegsPad               // Padding before pushing entire xmm regs
                      + calleeFPRegsSavedSize // pushed callee-saved float regs
                      // below calculated 'pad' will go here
                      + PSPSymSize                        // PSPSym
                      + compiler->lvaOutgoingArgSpaceSize // outgoing arg space
        ;

    unsigned pad = AlignmentPad(totalFrameSize, 16);

    genFuncletInfo.fiSpDelta = FPRegsPad                           // Padding to align SP on XMM_REGSIZE_BYTES boundary
                               + calleeFPRegsSavedSize             // Callee saved xmm regs
                               + pad + PSPSymSize                  // PSPSym
                               + compiler->lvaOutgoingArgSpaceSize // outgoing arg space
        ;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n");
        printf("Funclet prolog / epilog info\n");
        printf("   Function InitialSP-to-FP delta: %d\n", genFuncletInfo.fiFunction_InitialSP_to_FP_delta);
        printf("                         SP delta: %d\n", genFuncletInfo.fiSpDelta);
        printf("       PSP slot Initial SP offset: %d\n", genFuncletInfo.fiPSP_slot_InitialSP_offset);
    }

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        assert(genFuncletInfo.fiPSP_slot_InitialSP_offset ==
               compiler->lvaGetInitialSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main function and
                                                                              // funclet!
    }
#endif // DEBUG
}

#elif defined(TARGET_X86)

/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 *
 *
 *  Funclets have the following incoming arguments:
 *
 *      catch/filter-handler: eax = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:               eax = the exception object that was caught (see GT_CATCH_ARG)
 *      finally/fault:        none
 *
 *  Funclets set the following registers on exit:
 *
 *      catch/filter-handler: eax = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:               eax = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:        none
 *
 *  Funclet prolog/epilog sequence and funclet frame layout are TBD.
 *
 */

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // TODO We may need EBP restore sequence here if we introduce PSPSym

    // Add a padding for 16-byte alignment
    inst_RV_IV(INS_sub, REG_SPBASE, 12, EA_PTRSIZE);
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 */

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    // Revert a padding that was added for 16-byte alignment
    inst_RV_IV(INS_add, REG_SPBASE, 12, EA_PTRSIZE);

    instGen_Return(0);
}

/*****************************************************************************
 *
 *  Capture the information used to generate the funclet prologs and epilogs.
 */

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    if (!compiler->ehAnyFunclets())
    {
        return;
    }
}

#endif // TARGET_X86

void CodeGen::genSetPSPSym(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    noway_assert(isFramePointerUsed()); // We need an explicit frame pointer

#if defined(TARGET_AMD64)

    // The PSP sym value is Initial-SP, not Caller-SP!
    // We assume that RSP is Initial-SP when this function is called. That is, the stack frame
    // has been established.
    //
    // We generate:
    //     mov     [rbp-20h], rsp       // store the Initial-SP (our current rsp) in the PSPsym

    GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaPSPSym, 0);

#else // TARGET*

    NYI("Set function PSP sym");

#endif // TARGET*
}

#endif // FEATURE_EH_FUNCLETS

//-----------------------------------------------------------------------------
// genZeroInitFrameUsingBlockInit: architecture-specific helper for genZeroInitFrame in the case
// `genUseBlockInit` is set.
//
// Arguments:
//    untrLclHi      - (Untracked locals High-Offset)  The upper bound offset at which the zero init
//                                                     code will end initializing memory (not inclusive).
//    untrLclLo      - (Untracked locals Low-Offset)   The lower bound at which the zero init code will
//                                                     start zero initializing memory.
//    initReg        - A scratch register (that gets set to zero on some platforms).
//    pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'true' if this method sets initReg register to zero,
//                     'false' if initReg was set to a non-zero value, and left unchanged if initReg was not touched.
//
void CodeGen::genZeroInitFrameUsingBlockInit(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);
    assert(genUseBlockInit);
    assert(untrLclHi > untrLclLo);

    emitter*  emit        = GetEmitter();
    regNumber frameReg    = genFramePointerReg();
    regNumber zeroReg     = REG_NA;
    int       blkSize     = untrLclHi - untrLclLo;
    int       minSimdSize = XMM_REGSIZE_BYTES;

    assert(blkSize >= 0);
    noway_assert((blkSize % sizeof(int)) == 0);
    // initReg is not a live incoming argument reg
    assert((genRegMask(initReg) & intRegState.rsCalleeRegArgMaskLiveIn) == 0);

#if defined(TARGET_AMD64)
    // We will align on x64 so can use the aligned mov
    instruction simdMov = simdAlignedMovIns();
    // Aligning low we want to move up to next boundary
    int alignedLclLo = (untrLclLo + (XMM_REGSIZE_BYTES - 1)) & -XMM_REGSIZE_BYTES;

    if ((untrLclLo != alignedLclLo) && (blkSize < 2 * XMM_REGSIZE_BYTES))
    {
        // If unaligned and smaller then 2 x SIMD size we won't bother trying to align
        assert((alignedLclLo - untrLclLo) < XMM_REGSIZE_BYTES);
        simdMov = simdUnalignedMovIns();
    }
#else  // !defined(TARGET_AMD64)
    // We aren't going to try and align on x86
    instruction simdMov      = simdUnalignedMovIns();
    int         alignedLclLo = untrLclLo;
#endif // !defined(TARGET_AMD64)

    if (blkSize < minSimdSize)
    {
        zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

        int i = 0;
        for (; i + REGSIZE_BYTES <= blkSize; i += REGSIZE_BYTES)
        {
            emit->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, frameReg, untrLclLo + i);
        }
#if defined(TARGET_AMD64)
        assert((i == blkSize) || (i + (int)sizeof(int) == blkSize));
        if (i != blkSize)
        {
            emit->emitIns_AR_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, frameReg, untrLclLo + i);
            i += sizeof(int);
        }
#endif // defined(TARGET_AMD64)
        assert(i == blkSize);
    }
    else
    {
        // Grab a non-argument, non-callee saved XMM reg
        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef UNIX_AMD64_ABI
        // System V x64 first temp reg is xmm8
        regNumber zeroSIMDReg = genRegNumFromMask(RBM_XMM8);
#else
        // Windows first temp reg is xmm4
        regNumber zeroSIMDReg = genRegNumFromMask(RBM_XMM4);
#endif // UNIX_AMD64_ABI

#if defined(TARGET_AMD64)
        int alignedLclHi;
        int alignmentHiBlkSize;

        if ((blkSize < 2 * XMM_REGSIZE_BYTES) || (untrLclLo == alignedLclLo))
        {
            // Either aligned or smaller then 2 x SIMD size so we won't try to align
            // However, we still want to zero anything that is not in a 16 byte chunk at end
            int alignmentBlkSize = blkSize & -XMM_REGSIZE_BYTES;
            alignmentHiBlkSize   = blkSize - alignmentBlkSize;
            alignedLclHi         = untrLclLo + alignmentBlkSize;
            alignedLclLo         = untrLclLo;
            blkSize              = alignmentBlkSize;

            assert((blkSize + alignmentHiBlkSize) == (untrLclHi - untrLclLo));
        }
        else
        {
            // We are going to align

            // Aligning high we want to move down to previous boundary
            alignedLclHi = untrLclHi & -XMM_REGSIZE_BYTES;
            // Zero out the unaligned portions
            alignmentHiBlkSize     = untrLclHi - alignedLclHi;
            int alignmentLoBlkSize = alignedLclLo - untrLclLo;
            blkSize                = alignedLclHi - alignedLclLo;

            assert((blkSize + alignmentLoBlkSize + alignmentHiBlkSize) == (untrLclHi - untrLclLo));

            assert(alignmentLoBlkSize > 0);
            assert(alignmentLoBlkSize < XMM_REGSIZE_BYTES);
            assert((alignedLclLo - alignmentLoBlkSize) == untrLclLo);

            zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

            int i = 0;
            for (; i + REGSIZE_BYTES <= alignmentLoBlkSize; i += REGSIZE_BYTES)
            {
                emit->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, frameReg, untrLclLo + i);
            }
            assert((i == alignmentLoBlkSize) || (i + (int)sizeof(int) == alignmentLoBlkSize));
            if (i != alignmentLoBlkSize)
            {
                emit->emitIns_AR_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, frameReg, untrLclLo + i);
                i += sizeof(int);
            }

            assert(i == alignmentLoBlkSize);
        }
#else  // !defined(TARGET_AMD64)
        // While we aren't aligning the start, we still want to
        // zero anything that is not in a 16 byte chunk at end
        int alignmentBlkSize   = blkSize & -XMM_REGSIZE_BYTES;
        int alignmentHiBlkSize = blkSize - alignmentBlkSize;
        int alignedLclHi       = untrLclLo + alignmentBlkSize;
        blkSize                = alignmentBlkSize;

        assert((blkSize + alignmentHiBlkSize) == (untrLclHi - untrLclLo));
#endif // !defined(TARGET_AMD64)

        const int maxSimdSize = (int)compiler->roundDownSIMDSize(blkSize);
        assert((maxSimdSize >= XMM_REGSIZE_BYTES) && (maxSimdSize <= ZMM_REGSIZE_BYTES));

        // The loop is unrolled 3 times so we do not move to the loop block until it
        // will loop at least once so the threshold is 6.
        if (blkSize < (6 * maxSimdSize))
        {
            // Generate the following code:
            //
            //   xorps   xmm4, xmm4
            //   movups  xmmword ptr [ebp/esp-OFFS], xmm4
            //   ...
            //   movups  xmmword ptr [ebp/esp-OFFS], xmm4
            //   mov      qword ptr [ebp/esp-OFFS], rax
            //
            // NOTE: it implicitly zeroes YMM4 and ZMM4 as well.
            emit->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, zeroSIMDReg, zeroSIMDReg, zeroSIMDReg);

            int i = 0;
            if (maxSimdSize > XMM_REGSIZE_BYTES)
            {
                for (; i <= blkSize - maxSimdSize; i += maxSimdSize)
                {
                    // We previously aligned data to 16 bytes which might not be aligned to maxSimdSize
                    emit->emitIns_AR_R(simdUnalignedMovIns(), EA_ATTR(maxSimdSize), zeroSIMDReg, frameReg,
                                       alignedLclLo + i);
                }
                // Remainder will be handled by the xmm loop below
            }

            for (; i < blkSize; i += XMM_REGSIZE_BYTES)
            {
                emit->emitIns_AR_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, alignedLclLo + i);
            }

            assert(i == blkSize);
        }
        else
        {
            // Generate the following code:
            //
            //    xorps    xmm4, xmm4
            //    ;movaps xmmword ptr[ebp/esp-loOFFS], xmm4          ; alignment to 3x
            //    ;movaps xmmword ptr[ebp/esp-loOFFS + 10H], xmm4    ;
            //    mov rax, - <size>                                  ; start offset from hi
            //    movaps xmmword ptr[rbp + rax + hiOFFS      ], xmm4 ; <--+
            //    movaps xmmword ptr[rbp + rax + hiOFFS + 10H], xmm4 ;    |
            //    movaps xmmword ptr[rbp + rax + hiOFFS + 20H], xmm4 ;    | Loop
            //    add rax, 48                                        ;    |
            //    jne SHORT  -5 instr                                ; ---+

            emit->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, zeroSIMDReg, zeroSIMDReg, zeroSIMDReg);

            // How many extra don't fit into the 3x unroll
            int extraSimd = (blkSize % (XMM_REGSIZE_BYTES * 3)) / XMM_REGSIZE_BYTES;
            if (extraSimd != 0)
            {
                blkSize -= XMM_REGSIZE_BYTES;
                // Not a multiple of 3 so add stores at low end of block
                emit->emitIns_AR_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, alignedLclLo);
                if (extraSimd == 2)
                {
                    blkSize -= XMM_REGSIZE_BYTES;
                    // one more store needed
                    emit->emitIns_AR_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg,
                                       alignedLclLo + XMM_REGSIZE_BYTES);
                }
            }

            // Exact multiple of 3 simd lengths (or loop end condition will not be met)
            noway_assert((blkSize % (3 * XMM_REGSIZE_BYTES)) == 0);

            // At least 3 simd lengths remain (as loop is 3x unrolled and we want it to loop at least once)
            assert(blkSize >= (3 * XMM_REGSIZE_BYTES));
            // In range at start of loop
            assert((alignedLclHi - blkSize) >= untrLclLo);
            assert(((alignedLclHi - blkSize) + (XMM_REGSIZE_BYTES * 2)) < (untrLclHi - XMM_REGSIZE_BYTES));
            // In range at end of loop
            assert((alignedLclHi - (3 * XMM_REGSIZE_BYTES) + (2 * XMM_REGSIZE_BYTES)) <=
                   (untrLclHi - XMM_REGSIZE_BYTES));
            assert((alignedLclHi - (blkSize + extraSimd * XMM_REGSIZE_BYTES)) == alignedLclLo);

            // Set loop counter
            emit->emitIns_R_I(INS_mov, EA_PTRSIZE, initReg, -(ssize_t)blkSize);
            // Loop start
            emit->emitIns_ARX_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, initReg, 1, alignedLclHi);
            emit->emitIns_ARX_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, initReg, 1,
                                alignedLclHi + XMM_REGSIZE_BYTES);
            emit->emitIns_ARX_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, initReg, 1,
                                alignedLclHi + 2 * XMM_REGSIZE_BYTES);

            emit->emitIns_R_I(INS_add, EA_PTRSIZE, initReg, XMM_REGSIZE_BYTES * 3);
            // Loop until counter is 0
            emit->emitIns_J(INS_jne, nullptr, -5);

            // initReg will be zero at end of the loop
            *pInitRegZeroed = true;
        }

        if (untrLclHi != alignedLclHi)
        {
            assert(alignmentHiBlkSize > 0);
            assert(alignmentHiBlkSize < XMM_REGSIZE_BYTES);
            assert((alignedLclHi + alignmentHiBlkSize) == untrLclHi);

            zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

            int i = 0;
            for (; i + REGSIZE_BYTES <= alignmentHiBlkSize; i += REGSIZE_BYTES)
            {
                emit->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, frameReg, alignedLclHi + i);
            }
#if defined(TARGET_AMD64)
            assert((i == alignmentHiBlkSize) || (i + (int)sizeof(int) == alignmentHiBlkSize));
            if (i != alignmentHiBlkSize)
            {
                emit->emitIns_AR_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, frameReg, alignedLclHi + i);
                i += sizeof(int);
            }
#endif // defined(TARGET_AMD64)
            assert(i == alignmentHiBlkSize);
        }
    }
}

// Save compCalleeFPRegsPushed with the smallest register number saved at [RSP+offset], working
// down the stack to the largest register number stored at [RSP+offset-(genCountBits(regMask)-1)*XMM_REG_SIZE]
// Here offset = 16-byte aligned offset after pushing integer registers.
//
// Params
//   lclFrameSize - Fixed frame size excluding callee pushed int regs.
//             non-funclet: this will be compLclFrameSize.
//             funclet frames: this will be FuncletInfo.fiSpDelta.
void CodeGen::genPreserveCalleeSavedFltRegs(unsigned lclFrameSize)
{
    regMaskTP regMask = compiler->compCalleeFPRegsSavedMask;

    // Only callee saved floating point registers should be in regMask
    assert((regMask & RBM_FLT_CALLEE_SAVED) == regMask);

    if (GetEmitter()->ContainsCallNeedingVzeroupper() && !GetEmitter()->Contains256bitOrMoreAVX())
    {
        // The Intel optimization manual guidance in `3.11.5.3 Fixing Instruction Slowdowns` states:
        //   Insert a VZEROUPPER to tell the hardware that the state of the higher registers is clean
        //   between the VEX and the legacy SSE instructions. Often the best way to do this is to insert a
        //   VZEROUPPER before returning from any function that uses VEX (that does not produce a VEX
        //   register) and before any call to an unknown function.

        // This method contains a call that needs vzeroupper but also doesn't use 256-bit or higher
        // AVX itself. Thus we can optimize to only emitting a single vzeroupper in the function prologue
        // This reduces the overall amount of codegen, particularly for more common paths not using any
        // SIMD or floating-point.

        instGen(INS_vzeroupper);
    }

    // fast path return
    if (regMask == RBM_NONE)
    {
        return;
    }

#ifdef TARGET_AMD64
    unsigned firstFPRegPadding = compiler->lvaIsCalleeSavedIntRegCountEven() ? REGSIZE_BYTES : 0;
    unsigned offset            = lclFrameSize - firstFPRegPadding - XMM_REGSIZE_BYTES;

    // Offset is 16-byte aligned since we use movaps for preserving xmm regs.
    assert((offset % 16) == 0);
    instruction copyIns = ins_Copy(TYP_FLOAT);
#else  // !TARGET_AMD64
    unsigned    offset            = lclFrameSize - XMM_REGSIZE_BYTES;
    instruction copyIns           = INS_movupd;
#endif // !TARGET_AMD64

    for (regNumber reg = REG_FLT_CALLEE_SAVED_FIRST; regMask != RBM_NONE; reg = REG_NEXT(reg))
    {
        regMaskTP regBit = genRegMask(reg);
        if ((regBit & regMask) != 0)
        {
            // ABI requires us to preserve lower 128-bits of YMM register.
            GetEmitter()->emitIns_AR_R(copyIns, EA_16BYTE, reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
            regMask &= ~regBit;
            offset -= XMM_REGSIZE_BYTES;
        }
    }
}

// Save/Restore compCalleeFPRegsPushed with the smallest register number saved at [RSP+offset], working
// down the stack to the largest register number stored at [RSP+offset-(genCountBits(regMask)-1)*XMM_REG_SIZE]
// Here offset = 16-byte aligned offset after pushing integer registers.
//
// Params
//   lclFrameSize - Fixed frame size excluding callee pushed int regs.
//             non-funclet: this will be compLclFrameSize.
//             funclet frames: this will be FuncletInfo.fiSpDelta.
void CodeGen::genRestoreCalleeSavedFltRegs(unsigned lclFrameSize)
{
    regMaskTP regMask = compiler->compCalleeFPRegsSavedMask;

    // Only callee saved floating point registers should be in regMask
    assert((regMask & RBM_FLT_CALLEE_SAVED) == regMask);

    if (GetEmitter()->Contains256bitOrMoreAVX())
    {
        // The Intel optimization manual guidance in `3.11.5.3 Fixing Instruction Slowdowns` states:
        //   Insert a VZEROUPPER to tell the hardware that the state of the higher registers is clean
        //   between the VEX and the legacy SSE instructions. Often the best way to do this is to insert a
        //   VZEROUPPER before returning from any function that uses VEX (that does not produce a VEX
        //   register) and before any call to an unknown function.

        instGen(INS_vzeroupper);
    }

    // fast path return
    if (regMask == RBM_NONE)
    {
        return;
    }

#ifdef TARGET_AMD64
    unsigned    firstFPRegPadding = compiler->lvaIsCalleeSavedIntRegCountEven() ? REGSIZE_BYTES : 0;
    instruction copyIns           = ins_Copy(TYP_FLOAT);
#else  // !TARGET_AMD64
    unsigned    firstFPRegPadding = 0;
    instruction copyIns           = INS_movupd;
#endif // !TARGET_AMD64

    unsigned  offset;
    regNumber regBase;
    if (compiler->compLocallocUsed)
    {
        // localloc frame: use frame pointer relative offset
        assert(isFramePointerUsed());
        regBase = REG_FPBASE;
        offset  = lclFrameSize - genSPtoFPdelta() - firstFPRegPadding - XMM_REGSIZE_BYTES;
    }
    else
    {
        regBase = REG_SPBASE;
        offset  = lclFrameSize - firstFPRegPadding - XMM_REGSIZE_BYTES;
    }

#ifdef TARGET_AMD64
    // Offset is 16-byte aligned since we use movaps for restoring xmm regs
    assert((offset % 16) == 0);
#endif // TARGET_AMD64

    for (regNumber reg = REG_FLT_CALLEE_SAVED_FIRST; regMask != RBM_NONE; reg = REG_NEXT(reg))
    {
        regMaskTP regBit = genRegMask(reg);
        if ((regBit & regMask) != 0)
        {
            // ABI requires us to restore lower 128-bits of YMM register.
            GetEmitter()->emitIns_R_AR(copyIns, EA_16BYTE, reg, regBase, offset);
            regMask &= ~regBit;
            offset -= XMM_REGSIZE_BYTES;
        }
    }
}

//-----------------------------------------------------------------------------------
// instGen_MemoryBarrier: Emit a MemoryBarrier instruction
//
// Arguments:
//     barrierKind - kind of barrier to emit (Load-only is no-op on xarch)
//
// Notes:
//     All MemoryBarriers instructions can be removed by DOTNET_JitNoMemoryBarriers=1
//
void CodeGen::instGen_MemoryBarrier(BarrierKind barrierKind)
{
#ifdef DEBUG
    if (JitConfig.JitNoMemoryBarriers() == 1)
    {
        return;
    }
#endif // DEBUG

    // only full barrier needs to be emitted on Xarch
    if (barrierKind == BARRIER_FULL)
    {
        instGen(INS_lock);
        GetEmitter()->emitIns_I_AR(INS_or, EA_4BYTE, 0, REG_SPBASE, 0);
    }
}

#ifdef TARGET_AMD64
// Returns relocation type hint for an addr.
// Note that there are no reloc hints on x86.
//
// Arguments
//    addr  -  data address
//
// Returns
//    relocation type hint
//
unsigned short CodeGenInterface::genAddrRelocTypeHint(size_t addr)
{
    return compiler->eeGetRelocTypeHint((void*)addr);
}
#endif // TARGET_AMD64

// Return true if an absolute indirect data address can be encoded as IP-relative.
// offset. Note that this method should be used only when the caller knows that
// the address is an icon value that VM has given and there is no GenTree node
// representing it. Otherwise, one should always use FitsInAddrBase().
//
// Arguments
//    addr  -  an absolute indirect data address
//
// Returns
//    true if indir data addr could be encoded as IP-relative offset.
//
bool CodeGenInterface::genDataIndirAddrCanBeEncodedAsPCRelOffset(size_t addr)
{
#ifdef TARGET_AMD64
    return genAddrRelocTypeHint(addr) == IMAGE_REL_BASED_REL32;
#else
    // x86: PC-relative addressing is available only for control flow instructions (jmp and call)
    return false;
#endif
}

// Return true if an indirect code address can be encoded as IP-relative offset.
// Note that this method should be used only when the caller knows that the
// address is an icon value that VM has given and there is no GenTree node
// representing it. Otherwise, one should always use FitsInAddrBase().
//
// Arguments
//    addr  -  an absolute indirect code address
//
// Returns
//    true if indir code addr could be encoded as IP-relative offset.
//
bool CodeGenInterface::genCodeIndirAddrCanBeEncodedAsPCRelOffset(size_t addr)
{
#ifdef TARGET_AMD64
    return genAddrRelocTypeHint(addr) == IMAGE_REL_BASED_REL32;
#else
    // x86: PC-relative addressing is available only for control flow instructions (jmp and call)
    return true;
#endif
}

// Return true if an indirect code address can be encoded as 32-bit displacement
// relative to zero. Note that this method should be used only when the caller
// knows that the address is an icon value that VM has given and there is no
// GenTree node representing it. Otherwise, one should always use FitsInAddrBase().
//
// Arguments
//    addr  -  absolute indirect code address
//
// Returns
//    true if absolute indir code addr could be encoded as 32-bit displacement relative to zero.
//
bool CodeGenInterface::genCodeIndirAddrCanBeEncodedAsZeroRelOffset(size_t addr)
{
    return GenTreeIntConCommon::FitsInI32((ssize_t)addr);
}

// Return true if an absolute indirect code address needs a relocation recorded with VM.
//
// Arguments
//    addr  -  an absolute indirect code address
//
// Returns
//    true if indir code addr needs a relocation recorded with VM
//
bool CodeGenInterface::genCodeIndirAddrNeedsReloc(size_t addr)
{
    // If generating relocatable ngen code, then all code addr should go through relocation
    if (compiler->opts.compReloc)
    {
        return true;
    }

#ifdef TARGET_AMD64
    // See if the code indir addr can be encoded as 32-bit displacement relative to zero.
    // We don't need a relocation in that case.
    if (genCodeIndirAddrCanBeEncodedAsZeroRelOffset(addr))
    {
        return false;
    }

    // Else we need a relocation.
    return true;
#else  // TARGET_X86
    // On x86 there is no need to record or ask for relocations during jitting,
    // because all addrs fit within 32-bits.
    return false;
#endif // TARGET_X86
}

// Return true if a direct code address needs to be marked as relocatable.
//
// Arguments
//    addr  -  absolute direct code address
//
// Returns
//    true if direct code addr needs a relocation recorded with VM
//
bool CodeGenInterface::genCodeAddrNeedsReloc(size_t addr)
{
    // If generating relocatable ngen code, then all code addr should go through relocation
    if (compiler->opts.compReloc)
    {
        return true;
    }

#ifdef TARGET_AMD64
    // By default all direct code addresses go through relocation so that VM will setup
    // a jump stub if addr cannot be encoded as pc-relative offset.
    return true;
#else  // TARGET_X86
    // On x86 there is no need for recording relocations during jitting,
    // because all addrs fit within 32-bits.
    return false;
#endif // TARGET_X86
}

#endif // TARGET_XARCH
